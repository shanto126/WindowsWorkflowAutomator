using System.Collections.Concurrent;
using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.FileOrganizer;

public sealed class FileOrganizerService : IFileOrganizerService
{
    private const int MaxRecentActions = 100;
    private readonly IFileRuleService _rules;
    private readonly IAppLogger _logger;
    private readonly ConcurrentQueue<FileOrganizationActionLog> _recent = new();

    public FileOrganizerService(IFileRuleService rules, IAppLogger logger)
    {
        _rules = rules;
        _logger = logger;
    }

    public IReadOnlyList<FileOrganizationActionLog> RecentActions => _recent.ToArray();

    public event EventHandler<FileOrganizationActionLog>? ActionRecorded;

    public async Task<IReadOnlyList<FileOrganizationActionLog>> OrganizeFolderAsync(
        string folderPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return [Record(Failed(folderPath, null, "Watched folder does not exist."))];
        }

        var results = new List<FileOrganizationActionLog>();
        var files = await Task.Run(
            () => Directory.GetFiles(folderPath, "*", SearchOption.TopDirectoryOnly),
            cancellationToken);
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await OrganizeFileAsync(file, cancellationToken));
        }

        if (files.Length == 0)
        {
            results.Add(Record(Failed(folderPath, null, "No files found in the selected folder.")));
        }

        return results;
    }

    public async Task<FileOrganizationActionLog> OrganizeFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return Record(Failed(filePath, null, "File no longer exists."));
            }

            var fileName = Path.GetFileName(filePath);
            if (FileOrganizationHelpers.IsIncompleteFileName(fileName))
            {
                return Record(Failed(filePath, null, $"Skipped incomplete download: {fileName}"));
            }

            var ready = await FileStability.WaitUntilReadyAsync(filePath, TimeSpan.FromSeconds(30), cancellationToken);
            if (!ready)
            {
                return Record(Failed(filePath, null, $"File is locked or still downloading: {fileName}"));
            }

            if (!File.Exists(filePath))
            {
                return Record(Failed(filePath, null, "File disappeared before it could be organized."));
            }

            var rules = await _rules.GetAllAsync(cancellationToken);
            var rule = FileOrganizationHelpers.FindMatchingRule(rules, filePath);
            if (rule is null)
            {
                return Record(Failed(filePath, null, $"No matching rule for {fileName}."));
            }

            var destinationFolder = string.IsNullOrWhiteSpace(rule.DestinationFolder)
                ? Path.GetDirectoryName(filePath) ?? string.Empty
                : rule.DestinationFolder.Trim();

            if (string.IsNullOrWhiteSpace(destinationFolder))
            {
                return Record(Failed(filePath, null, "Destination folder is missing."));
            }

            if (!Directory.Exists(destinationFolder))
            {
                try
                {
                    Directory.CreateDirectory(destinationFolder);
                }
                catch (Exception ex)
                {
                    _logger.Error("Could not create destination folder.", ex);
                    return Record(Failed(filePath, null, $"Destination folder is missing: {destinationFolder}"));
                }
            }

            var targetName = FileOrganizationHelpers.BuildTargetFileName(filePath, rule);
            var destinationPath = FileOrganizationHelpers.GetAvailablePath(destinationFolder, targetName);

            if (PathsEqual(filePath, destinationPath))
            {
                return Record(Failed(filePath, destinationPath, $"{fileName} is already in the destination folder."));
            }

            switch (rule.Action)
            {
                case FileOrganizationAction.Copy:
                    File.Copy(filePath, destinationPath);
                    break;
                case FileOrganizationAction.Rename:
                case FileOrganizationAction.Move:
                    File.Move(filePath, destinationPath);
                    break;
                default:
                    return Record(Failed(filePath, null, $"Unsupported action: {rule.Action}"));
            }

            var verb = rule.Action.ToString().ToLowerInvariant();
            var message = $"{fileName} {verb}d to {destinationPath}";
            _logger.Information(message);
            return Record(new FileOrganizationActionLog
            {
                Succeeded = true,
                SourcePath = filePath,
                DestinationPath = destinationPath,
                Message = message
            });
        }
        catch (IOException ex)
        {
            _logger.Error("File organization I/O failed.", ex);
            return Record(Failed(filePath, null, $"Could not organize file (locked or in use): {Path.GetFileName(filePath)}"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.Error("File organization access denied.", ex);
            return Record(Failed(filePath, null, $"Access denied: {Path.GetFileName(filePath)}"));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error("File organization failed.", ex);
            return Record(Failed(filePath, null, $"Unexpected error organizing {Path.GetFileName(filePath)}."));
        }
    }

    private FileOrganizationActionLog Record(FileOrganizationActionLog entry)
    {
        _recent.Enqueue(entry);
        while (_recent.Count > MaxRecentActions && _recent.TryDequeue(out _))
        {
        }

        ActionRecorded?.Invoke(this, entry);
        return entry;
    }

    private static FileOrganizationActionLog Failed(string? source, string? destination, string message) =>
        new()
        {
            Succeeded = false,
            SourcePath = source ?? string.Empty,
            DestinationPath = destination,
            Message = message
        };

    private static bool PathsEqual(string left, string right) =>
        string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
}
