using System.Collections.Concurrent;
using WindowsWorkflowAutomator.Logging;

namespace WindowsWorkflowAutomator.FileOrganizer;

public sealed class DownloadFolderMonitor : IDownloadFolderMonitor
{
    private readonly IFileOrganizerService _organizer;
    private readonly IAppLogger _logger;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _pending = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();
    private FileSystemWatcher? _watcher;
    private CancellationTokenSource _lifetime = new();

    public DownloadFolderMonitor(IFileOrganizerService organizer, IAppLogger logger)
    {
        _organizer = organizer;
        _logger = logger;
    }

    public bool IsRunning { get; private set; }

    public string? WatchedFolder { get; private set; }

    public void Start(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("Choose a folder to watch.", nameof(folderPath));
        }

        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Folder does not exist: {folderPath}");
        }

        lock (_sync)
        {
            StopCore();
            _lifetime.Dispose();
            _lifetime = new CancellationTokenSource();
            WatchedFolder = Path.GetFullPath(folderPath);

            _watcher = new FileSystemWatcher(WatchedFolder)
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
                Filter = "*.*",
                InternalBufferSize = 64 * 1024
            };

            _watcher.Created += OnChanged;
            _watcher.Changed += OnChanged;
            _watcher.Renamed += OnRenamed;
            _watcher.Error += OnError;
            _watcher.EnableRaisingEvents = true;
            IsRunning = true;
            _logger.Information($"Download folder monitor started: {WatchedFolder}");
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            StopCore();
        }
    }

    public void Dispose()
    {
        Stop();
        _lifetime.Dispose();
    }

    private void StopCore()
    {
        if (_watcher is not null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Created -= OnChanged;
            _watcher.Changed -= OnChanged;
            _watcher.Renamed -= OnRenamed;
            _watcher.Error -= OnError;
            _watcher.Dispose();
            _watcher = null;
        }

        foreach (var cts in _pending.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }

        _pending.Clear();
        _lifetime.Cancel();
        IsRunning = false;
        WatchedFolder = null;
    }

    private void OnRenamed(object sender, RenamedEventArgs e) => Schedule(e.FullPath);

    private void OnChanged(object sender, FileSystemEventArgs e) => Schedule(e.FullPath);

    private void OnError(object sender, ErrorEventArgs e) =>
        _logger.Error("Download folder monitor watcher error.", e.GetException());

    private void Schedule(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || Directory.Exists(path))
        {
            return;
        }

        if (FileOrganizationHelpers.IsIncompleteFileName(Path.GetFileName(path)))
        {
            return;
        }

        var debounce = new CancellationTokenSource();
        _pending.AddOrUpdate(path, debounce, (_, existing) =>
        {
            existing.Cancel();
            return debounce;
        });

        _ = ProcessAfterDebounceAsync(path, debounce);
    }

    private async Task ProcessAfterDebounceAsync(string path, CancellationTokenSource debounce)
    {
        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(debounce.Token, _lifetime.Token);
            await Task.Delay(800, linked.Token);
            await _organizer.OrganizeFileAsync(path, linked.Token);
        }
        catch (OperationCanceledException)
        {
            // Debounced or monitor stopped.
        }
        catch (Exception ex)
        {
            _logger.Error("Download folder monitor failed to organize a file.", ex);
        }
        finally
        {
            if (_pending.TryGetValue(path, out var current) && current == debounce)
            {
                _pending.TryRemove(path, out _);
                debounce.Dispose();
            }
        }
    }
}
