using System.Diagnostics;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WindowsWorkflowAutomator.Configuration;
using WindowsWorkflowAutomator.Data;
using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Models;
using WindowsWorkflowAutomator.Repositories;
using WindowsWorkflowAutomator.Security;

namespace WindowsWorkflowAutomator.GitHub;

public sealed class GitHubService : IGitHubService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAppSettingsService _settings;
    private readonly ISecretProtector _secretProtector;
    private readonly IAppLogger _logger;
    private AutoSyncService? _autoSyncService; // managed per service lifetime
    private readonly object _autoSyncSync = new();

    public GitHubService(
        IServiceScopeFactory scopeFactory,
        IAppSettingsService settings,
        ISecretProtector secretProtector,
        IAppLogger logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings;
        _secretProtector = secretProtector;
        _logger = logger;
    }

    public async Task<GitHubRepositorySnapshot?> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var config = await GetConfigurationEntityAsync(cancellationToken);
        if (config is null)
        {
            return null;
        }

        var snapshot = new GitHubRepositorySnapshot
        {
            LocalPath = config.LocalPath,
            RemoteUrl = config.RemoteUrl,
            CommitMessageTemplate = config.CommitMessageTemplate,
            HasPersonalAccessToken = !string.IsNullOrWhiteSpace(_settings.Current.GitHubPatProtected),
            SyncMode = NormalizeSyncMode(config.SyncMode),
            InactivitySeconds = config.InactivitySeconds > 0 ? config.InactivitySeconds : 30
        };

        if (snapshot.SyncMode == GitHubSyncMode.SmartAutoSync && !string.IsNullOrWhiteSpace(snapshot.LocalPath))
        {
            StartAutoSync(snapshot.LocalPath, snapshot.InactivitySeconds);
        }

        return snapshot;
    }

    public async Task<GitHubOperationResult> ConfigureRepositoryAsync(
        string localPath,
        string remoteUrl,
        string? commitMessageTemplate = null,
        string? personalAccessToken = null,
        string? syncMode = null,
        int? inactivitySeconds = null,
        CancellationToken cancellationToken = default)
    {
        localPath = localPath?.Trim() ?? string.Empty;
        remoteUrl = remoteUrl?.Trim() ?? string.Empty;
        syncMode = NormalizeSyncMode(syncMode);
        commitMessageTemplate = string.IsNullOrWhiteSpace(commitMessageTemplate)
            ? "chore: backup changes"
            : commitMessageTemplate.Trim();

        if (localPath.Length == 0 || !Directory.Exists(localPath))
        {
            return await FailAndLogAsync(
                GitHubOperationStatus.InvalidRepository,
                "Local repository folder does not exist.",
                cancellationToken);
        }

        if (!Uri.TryCreate(remoteUrl, UriKind.Absolute, out var remoteUri)
            || (remoteUri.Scheme != Uri.UriSchemeHttps && remoteUri.Scheme != Uri.UriSchemeHttp))
        {
            return await FailAndLogAsync(
                GitHubOperationStatus.InvalidRemote,
                "Enter a valid remote URL (http/https).",
                cancellationToken);
        }

        var gitAvailable = await EnsureGitAvailableAsync(cancellationToken);
        if (!gitAvailable.Succeeded)
        {
            return gitAvailable;
        }

        var isRepo = await RunGitAsync(localPath, ["rev-parse", "--is-inside-work-tree"], cancellationToken);
        if (!isRepo.Succeeded)
        {
            return await FailAndLogAsync(
                GitHubOperationStatus.InvalidRepository,
                "The selected folder is not a valid git repository.",
                cancellationToken,
                [isRepo.Error]);
        }

        var hasOrigin = await RunGitAsync(localPath, ["remote", "get-url", "origin"], cancellationToken);
        var remoteArgs = hasOrigin.Succeeded
            ? new[] { "remote", "set-url", "origin", remoteUrl }
            : new[] { "remote", "add", "origin", remoteUrl };

        var remoteSet = await RunGitAsync(localPath, remoteArgs, cancellationToken);
        if (!remoteSet.Succeeded)
        {
            var mapped = MapStatusFromGitText(remoteSet.Error);
            return await FailAndLogAsync(
                mapped,
                "Could not configure git remote origin.",
                cancellationToken,
                [remoteSet.Error]);
        }

        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var config = await db.GitHubRepositories.OrderBy(x => x.Id).FirstOrDefaultAsync(cancellationToken);
            if (config is null)
            {
                config = new GitHubRepository();
                db.GitHubRepositories.Add(config);
            }

            config.LocalPath = localPath;
            config.RemoteUrl = remoteUrl;
            config.CommitMessageTemplate = commitMessageTemplate;
            config.SyncMode = syncMode;

            if (inactivitySeconds.HasValue)
            {
                config.InactivitySeconds = inactivitySeconds.Value;
            }

            config.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        // Start/stop auto-sync according to saved configuration
        try
        {
            var saved = await GetConfigurationAsync(cancellationToken);
            if (saved is not null && saved.SyncMode == GitHubSyncMode.SmartAutoSync && !string.IsNullOrWhiteSpace(saved.LocalPath))
            {
                StartAutoSync(saved.LocalPath, saved.InactivitySeconds);
            }
            else
            {
                StopAutoSync();
            }
        }
        catch { /* keep configure resilient */ }

        if (personalAccessToken is not null)
        {
            _settings.Current.GitHubPatProtected = string.IsNullOrWhiteSpace(personalAccessToken)
                ? string.Empty
                : _secretProtector.Protect(personalAccessToken.Trim());
            _settings.Save();
        }

        await AddActivityAsync("Information", "GitHub", $"Repository configured: {localPath}", cancellationToken);
        _logger.Information($"GitHub repository configured: {localPath}");
        return GitHubOperationResult.Ok("Repository configuration saved.");
    }

    public async Task<GitHubStatusResult> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var gitAvailable = await EnsureGitAvailableAsync(cancellationToken);
        if (!gitAvailable.Succeeded)
        {
            return GitHubStatusResult.FromFailure(gitAvailable);
        }

        var config = await GetConfigurationEntityAsync(cancellationToken);
        if (config is null)
        {
            return GitHubStatusResult.FromFailure(GitHubOperationResult.Fail(
                GitHubOperationStatus.NotConfigured,
                "Configure a repository first."));
        }

        return await GetStatusForPathAsync(config.LocalPath, cancellationToken);
    }

    public async Task<GitHubStatusResult> GetStatusForRepositoryAsync(
        string localPath,
        CancellationToken cancellationToken = default)
    {
        var gitAvailable = await EnsureGitAvailableAsync(cancellationToken);
        if (!gitAvailable.Succeeded)
        {
            return GitHubStatusResult.FromFailure(gitAvailable);
        }

        return await GetStatusForPathAsync(localPath, cancellationToken);
    }

    public async Task<string?> GetRemoteUrlAsync(
        string localPath,
        CancellationToken cancellationToken = default)
    {
        var remote = await RunGitAsync(localPath, ["remote", "get-url", "origin"], cancellationToken);
        return remote.Succeeded && !string.IsNullOrWhiteSpace(remote.Output)
            ? remote.Output.Trim()
            : null;
    }

    public async Task<GitHubOperationResult> InitializeRepositoryAsync(
        string localPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(localPath) || !Directory.Exists(localPath))
        {
            return GitHubOperationResult.Fail(GitHubOperationStatus.InvalidRepository, "The selected folder does not exist.");
        }

        var result = await RunGitAsync(localPath, ["init"], cancellationToken);
        return result.Succeeded
            ? GitHubOperationResult.Ok("Git repository initialized.")
            : GitHubOperationResult.Fail(GitHubOperationStatus.InvalidRepository, "Could not initialize the Git repository.", [result.Error]);
    }

    public async Task SetSelectedRepositoryAsync(
        string localPath,
        CancellationToken cancellationToken = default)
    {
        localPath = localPath.Trim();
        var remoteUrl = await GetRemoteUrlAsync(localPath, cancellationToken) ?? string.Empty;
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var config = await db.GitHubRepositories.OrderBy(x => x.Id).FirstOrDefaultAsync(cancellationToken);
        if (config is null)
        {
            return;
        }

        config.LocalPath = localPath;
        config.RemoteUrl = remoteUrl;
        config.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<GitHubStatusResult> GetStatusForPathAsync(
        string localPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(localPath) || !Directory.Exists(localPath))
        {
            return GitHubStatusResult.FromFailure(GitHubOperationResult.Fail(
                GitHubOperationStatus.InvalidRepository,
                "The selected repository folder does not exist."));
        }

        var status = await RunGitAsync(localPath, ["status", "--porcelain"], cancellationToken);
        if (!status.Succeeded)
        {
            var mapped = MapStatusFromGitText(status.Error);
            var failure = await FailAndLogAsync(mapped, "Could not read git status.", cancellationToken, [status.Error]);
            return GitHubStatusResult.FromFailure(failure);
        }

        var files = ParseChangedFiles(status.Output);
        return new GitHubStatusResult
        {
            Succeeded = true,
            Status = GitHubOperationStatus.Success,
            Message = files.Count == 0
                ? "No changes detected."
                : $"{files.Count} changed file(s) detected.",
            ChangedFilesCount = files.Count,
            ChangedFiles = files
        };
    }

    public async Task<GitHubOperationResult> CommitAsync(string message, CancellationToken cancellationToken = default)
    {
        var gitAvailable = await EnsureGitAvailableAsync(cancellationToken);
        if (!gitAvailable.Succeeded)
        {
            return gitAvailable;
        }

        var config = await GetConfigurationEntityAsync(cancellationToken);
        if (config is null)
        {
            return GitHubOperationResult.Fail(GitHubOperationStatus.NotConfigured, "Configure a repository first.");
        }

        var status = await GetStatusAsync(cancellationToken);
        if (!status.Succeeded)
        {
            return status;
        }

        if (status.ChangedFilesCount == 0)
        {
            return GitHubOperationResult.Fail(GitHubOperationStatus.NoChanges, "No changes to commit.");
        }

        var addResult = await RunGitAsync(config.LocalPath, ["add", "-A"], cancellationToken);
        if (!addResult.Succeeded)
        {
            var mapped = MapStatusFromGitText(addResult.Error);
            return await FailAndLogAsync(mapped, "Failed to stage changes.", cancellationToken, [addResult.Error]);
        }

        var commitMessage = string.IsNullOrWhiteSpace(message)
            ? config.CommitMessageTemplate
            : message.Trim();
        var commitResult = await RunGitAsync(config.LocalPath, ["commit", "-m", commitMessage], cancellationToken);
        if (!commitResult.Succeeded)
        {
            var lower = (commitResult.Output + Environment.NewLine + commitResult.Error).ToLowerInvariant();
            if (lower.Contains("nothing to commit"))
            {
                return GitHubOperationResult.Fail(GitHubOperationStatus.NoChanges, "No changes to commit.");
            }

            var mapped = MapStatusFromGitText(commitResult.Error);
            return await FailAndLogAsync(mapped, "Commit failed.", cancellationToken, [commitResult.Error]);
        }

        await AddActivityAsync("Information", "GitHub", $"Local commit created: {commitMessage}", cancellationToken);
        _logger.Information($"Git commit created with message: {commitMessage}");
        return GitHubOperationResult.Ok("Commit created locally.", new[] { commitResult.Output });
    }

    public async Task<GitHubOperationResult> PushAsync(CancellationToken cancellationToken = default)
    {
        var gitAvailable = await EnsureGitAvailableAsync(cancellationToken);
        if (!gitAvailable.Succeeded)
        {
            return gitAvailable;
        }

        var config = await GetConfigurationEntityAsync(cancellationToken);
        if (config is null)
        {
            return GitHubOperationResult.Fail(GitHubOperationStatus.NotConfigured, "Configure a repository first.");
        }

        var branchValidation = await ValidateCurrentBranchAsync(config.LocalPath, cancellationToken);
        if (!branchValidation.Result.Succeeded)
        {
            return branchValidation.Result;
        }

        var remoteUrl = await GetRemoteUrlAsync(config.LocalPath, cancellationToken);
        if (string.IsNullOrWhiteSpace(remoteUrl))
        {
            return await FailAndLogAsync(
                GitHubOperationStatus.InvalidRemote,
                "No GitHub remote URL is configured for this repository.",
                cancellationToken);
        }

        var token = GetToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return await FailAndLogAsync(
                GitHubOperationStatus.AuthFailure,
                "GitHub Personal Access Token is not configured.",
                cancellationToken);
        }

        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes($"x-access-token:{token}"));
        _logger.Information(
            $"GitHub push target - Current repository: {config.LocalPath}; " +
            $"Current branch: {branchValidation.Branch}; Remote URL: {remoteUrl}");
        var pushArgs = new[]
        {
            "-c",
            $"http.extraHeader=AUTHORIZATION: basic {header}",
            "push",
            "origin",
            branchValidation.Branch
        };

        var push = await RunGitAsync(config.LocalPath, pushArgs, cancellationToken);
        if (!push.Succeeded)
        {
            var mapped = MapStatusFromGitText(push.Error + Environment.NewLine + push.Output);
            return await FailAndLogAsync(mapped, "Push failed.", cancellationToken, [push.Error, push.Output]);
        }

        await AddActivityAsync("Information", "GitHub", $"Pushed branch '{branchValidation.Branch}' to origin.", cancellationToken);
        _logger.Information($"Git push completed for branch '{branchValidation.Branch}'.");
        return GitHubOperationResult.Ok("Push completed successfully.", new[] { push.Output });
    }

    private async Task<GitHubRepository?> GetConfigurationEntityAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.GitHubRepositories
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<GitHubOperationResult> EnsureGitAvailableAsync(CancellationToken cancellationToken)
    {
        var result = await RunGitAsync(Environment.CurrentDirectory, ["--version"], cancellationToken);
        if (result.Succeeded)
        {
            return GitHubOperationResult.Ok("git is available.");
        }

        return await FailAndLogAsync(
            GitHubOperationStatus.GitNotInstalled,
            "git executable is not available on this machine.",
            cancellationToken,
            [result.Error]);
    }

    private string GetToken()
    {
        try
        {
            return _secretProtector.Unprotect(_settings.Current.GitHubPatProtected);
        }
        catch (Exception ex)
        {
            _logger.Error("Could not read GitHub token from secure settings.", ex);
            return string.Empty;
        }
    }

    private async Task<(GitHubOperationResult Result, string Branch)> ValidateCurrentBranchAsync(
        string localPath,
        CancellationToken cancellationToken)
    {
        var currentBranch = await RunGitAsync(localPath, ["branch", "--show-current"], cancellationToken);
        var branch = currentBranch.Output.Trim();
        if (!currentBranch.Succeeded || string.IsNullOrWhiteSpace(branch))
        {
            return (await FailAndLogAsync(
                GitHubOperationStatus.InvalidBranch,
                "No active git branch found. Please create a commit first.",
                cancellationToken,
                [currentBranch.Output, currentBranch.Error]), string.Empty);
        }

        var branchCheck = await RunGitAsync(localPath, ["show-ref", "--verify", $"refs/heads/{branch}"], cancellationToken);
        if (!branchCheck.Succeeded)
        {
            return (await FailAndLogAsync(
                GitHubOperationStatus.InvalidBranch,
                "The current git branch is not available locally.",
                cancellationToken,
                [branchCheck.Error]), string.Empty);
        }

        return (GitHubOperationResult.Ok("Local branch is valid."), branch);
    }

    private static string NormalizeSyncMode(string? syncMode) => syncMode switch
    {
        GitHubSyncMode.SmartAutoSync or "Smart Auto Sync" => GitHubSyncMode.SmartAutoSync,
        GitHubSyncMode.Scheduled or "Scheduled (Coming Soon)" => GitHubSyncMode.Scheduled,
        _ => GitHubSyncMode.Manual
    };

    private async Task<GitHubOperationResult> FailAndLogAsync(
        GitHubOperationStatus status,
        string message,
        CancellationToken cancellationToken,
        IReadOnlyList<string>? details = null)
    {
        await AddActivityAsync("Error", "GitHub", message, cancellationToken);
        _logger.Warning(message);
        return GitHubOperationResult.Fail(status, message, details);
    }

    // Exposed to allow AutoSyncService to log activity via the service interface
    public async Task LogActivityAsync(string level, string category, string message, CancellationToken cancellationToken = default)
    {
        await AddActivityAsync(level, category, message, cancellationToken);
    }

    private async Task AddActivityAsync(string level, string category, string message, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var activityLogs = scope.ServiceProvider.GetRequiredService<IActivityLogRepository>();
        await activityLogs.AddAsync(new ActivityLogEntry
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Level = level,
            Category = category,
            Message = message
        }, cancellationToken);
    }

    private static List<string> ParseChangedFiles(string porcelain)
    {
        if (string.IsNullOrWhiteSpace(porcelain))
        {
            return new List<string>();
        }

        var lines = porcelain
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Length >= 4 ? line[3..].Trim() : line.Trim())
            .Where(line => line.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return lines;
    }

    private static GitHubOperationStatus MapStatusFromGitText(string text)
    {
        var source = (text ?? string.Empty).ToLowerInvariant();
        if (source.Contains("not a git repository"))
        {
            return GitHubOperationStatus.InvalidRepository;
        }

        if (source.Contains("authentication failed")
            || source.Contains("could not read username")
            || source.Contains("invalid username or password")
            || source.Contains("403"))
        {
            return GitHubOperationStatus.AuthFailure;
        }

        if (source.Contains("non-fast-forward")
            || source.Contains("fetch first")
            || source.Contains("rejected")
            || source.Contains("failed to push some refs"))
        {
            return GitHubOperationStatus.MergeConflict;
        }

        if (source.Contains("could not resolve host")
            || source.Contains("failed to connect")
            || source.Contains("network is unreachable")
            || source.Contains("connection timed out"))
        {
            return GitHubOperationStatus.NetworkFailure;
        }

        if (source.Contains("could not find remote branch")
            || source.Contains("unknown revision"))
        {
            return GitHubOperationStatus.InvalidBranch;
        }

        if (source.Contains("is not recognized"))
        {
            return GitHubOperationStatus.GitNotInstalled;
        }

        return GitHubOperationStatus.Error;
    }

    private static async Task<GitCommandResult> RunGitAsync(
        string workingDirectory,
        IReadOnlyList<string> args,
        CancellationToken cancellationToken)
    {
        try
        {
            var info = new ProcessStartInfo
            {
                FileName = "git",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var arg in args)
            {
                info.ArgumentList.Add(arg);
            }

            using var process = new Process { StartInfo = info };
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var output = await outputTask;
            var error = await errorTask;

            return new GitCommandResult(
                process.ExitCode == 0,
                output.Trim(),
                error.Trim());
        }
        catch (Exception ex)
        {
            return new GitCommandResult(false, string.Empty, ex.Message);
        }
    }

    private readonly record struct GitCommandResult(bool Succeeded, string Output, string Error);

    // Expose convenience wrapper required by interface
    public Task<GitHubRepositorySnapshot?> GetConfigurationWithSyncAsync(CancellationToken cancellationToken = default)
    {
        return GetConfigurationAsync(cancellationToken);
    }

    private void StartAutoSync(string localPath, int inactivitySeconds)
    {
        lock (_autoSyncSync)
        {
            try
            {
                if (_autoSyncService is not null)
                {
                    // If already running for same path, update inactivity and return
                    if (string.Equals(_autoSyncService.WatchedFolder, Path.GetFullPath(localPath), StringComparison.OrdinalIgnoreCase))
                    {
                        // restart with updated inactivity
                        _autoSyncService.Stop();
                        _autoSyncService.Start(localPath, inactivitySeconds);
                        return;
                    }

                    _autoSyncService.Dispose();
                    _autoSyncService = null;
                }

                _autoSyncService = new AutoSyncService(this, _logger);
                _autoSyncService.Start(localPath, inactivitySeconds);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to start auto-sync.", ex);
            }
        }
    }

    private void StopAutoSync()
    {
        lock (_autoSyncSync)
        {
            try
            {
                if (_autoSyncService is not null)
                {
                    _autoSyncService.Dispose();
                    _autoSyncService = null;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to stop auto-sync.", ex);
            }
        }
    }
}

