using System.Collections.Concurrent;
using WindowsWorkflowAutomator.Logging;

namespace WindowsWorkflowAutomator.GitHub;

public sealed class AutoSyncService : IDisposable
{
    private readonly IGitHubService _gitHubService;
    private readonly IAppLogger _logger;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _pending = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();
    private FileSystemWatcher? _watcher;
    private CancellationTokenSource _lifetime = new();
    private string? _watchedFolder;
    private int _inactivitySeconds = 30;

    public AutoSyncService(IGitHubService gitHubService, IAppLogger logger)
    {
        _gitHubService = gitHubService;
        _logger = logger;
    }

    public bool IsRunning { get; private set; }

    public string? WatchedFolder => _watchedFolder;

    public void Start(string folderPath, int inactivitySeconds)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentException("Folder path required", nameof(folderPath));

        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException(folderPath);

        lock (_sync)
        {
            StopCore();
            _lifetime.Dispose();
            _lifetime = new CancellationTokenSource();
            _watchedFolder = Path.GetFullPath(folderPath);
            _inactivitySeconds = inactivitySeconds <= 0 ? 30 : inactivitySeconds;

            _watcher = new FileSystemWatcher(_watchedFolder)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
                Filter = "*.*",
                InternalBufferSize = 64 * 1024
            };

            _watcher.Created += OnChanged;
            _watcher.Changed += OnChanged;
            _watcher.Renamed += OnRenamed;
            _watcher.Deleted += OnChanged;
            _watcher.Error += OnError;
            _watcher.EnableRaisingEvents = true;
            IsRunning = true;
            _logger.Information($"GitHub auto-sync started: {_watchedFolder} (inactivity {_inactivitySeconds}s)");
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            StopCore();
        }
    }

    private void StopCore()
    {
        if (_watcher is not null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Created -= OnChanged;
            _watcher.Changed -= OnChanged;
            _watcher.Renamed -= OnRenamed;
            _watcher.Deleted -= OnChanged;
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
        _watchedFolder = null;
        IsRunning = false;
    }

    public void Dispose()
    {
        Stop();
        _lifetime.Dispose();
    }

    private void OnRenamed(object sender, RenamedEventArgs e) => Schedule(e.FullPath);
    private void OnChanged(object sender, FileSystemEventArgs e) => Schedule(e.FullPath);
    private void OnError(object sender, ErrorEventArgs e) => _logger.Error("GitHub auto-sync watcher error.", e.GetException());

    private void Schedule(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            // ignore directories
            if (Directory.Exists(path))
                return;

            // ignore files in .git folder or common build outputs
            var lower = path.ToLowerInvariant();
            if (lower.Contains(".git") || lower.Contains("\\bin\\") || lower.Contains("\\obj\\") || lower.EndsWith(".dll") || lower.EndsWith(".exe"))
                return;

            var debounce = new CancellationTokenSource();
            _pending.AddOrUpdate(path, debounce, (_, existing) =>
            {
                existing.Cancel();
                existing.Dispose();
                return debounce;
            });

            _ = ProcessAfterDebounceAsync(path, debounce);
        }
        catch (Exception ex)
        {
            _logger.Error("AutoSync schedule failed.", ex);
        }
    }

    private async Task ProcessAfterDebounceAsync(string path, CancellationTokenSource debounce)
    {
        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(debounce.Token, _lifetime.Token);
            await Task.Delay(_inactivitySeconds * 1000, linked.Token);

            // After inactivity, check git status and commit if needed
            var status = await _gitHubService.GetStatusAsync(linked.Token);
            if (!status.Succeeded || status.ChangedFilesCount == 0)
            {
                _logger.Information("AutoSync: no changes to commit after debounce.");
                return;
            }

            var message = SmartCommitMessageGenerator.GenerateMessage(status.ChangedFiles);
            var commitResult = await _gitHubService.CommitAsync(message, linked.Token);
            if (commitResult.Succeeded)
            {
                await _gitHubService.LogActivityAsync("Information", "GitHub", $"Auto-commit: {message}", linked.Token);
                _logger.Information($"Auto-sync committed: {message}");
            }
            else
            {
                _logger.Warning($"Auto-sync commit failed: {commitResult.Message}");
            }
        }
        catch (OperationCanceledException)
        {
            // canceled by new change or stop
        }
        catch (Exception ex)
        {
            _logger.Error("AutoSync processing failed.", ex);
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