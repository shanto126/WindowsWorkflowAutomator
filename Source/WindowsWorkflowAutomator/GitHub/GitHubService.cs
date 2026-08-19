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

        return new GitHubRepositorySnapshot
        {
            LocalPath = config.LocalPath,
            RemoteUrl = config.RemoteUrl,
            Branch = config.Branch,
            CommitMessageTemplate = config.CommitMessageTemplate,
            HasPersonalAccessToken = !string.IsNullOrWhiteSpace(_settings.Current.GitHubPatProtected)
        };
    }

    public async Task<GitHubOperationResult> ConfigureRepositoryAsync(
        string localPath,
        string remoteUrl,
        string branch,
        string? commitMessageTemplate = null,
        string? personalAccessToken = null,
        CancellationToken cancellationToken = default)
    {
        localPath = localPath?.Trim() ?? string.Empty;
        remoteUrl = remoteUrl?.Trim() ?? string.Empty;
        branch = branch?.Trim() ?? string.Empty;
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

        if (branch.Length == 0)
        {
            return await FailAndLogAsync(
                GitHubOperationStatus.InvalidBranch,
                "Branch name is required.",
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

        var branchExists = await RunGitAsync(localPath, ["show-ref", "--verify", $"refs/heads/{branch}"], cancellationToken);
        if (!branchExists.Succeeded)
        {
            return await FailAndLogAsync(
                GitHubOperationStatus.InvalidBranch,
                $"Branch '{branch}' does not exist locally.",
                cancellationToken,
                [branchExists.Error]);
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
            config.Branch = branch;
            config.CommitMessageTemplate = commitMessageTemplate;
            config.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        if (personalAccessToken is not null)
        {
            _settings.Current.GitHubPatProtected = string.IsNullOrWhiteSpace(personalAccessToken)
                ? string.Empty
                : _secretProtector.Protect(personalAccessToken.Trim());
            _settings.Save();
        }

        await AddActivityAsync("Information", "GitHub", $"Repository configured: {localPath} ({branch})", cancellationToken);
        _logger.Information($"GitHub repository configured: {localPath} ({branch})");
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

        var status = await RunGitAsync(config.LocalPath, ["status", "--porcelain"], cancellationToken);
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
                ? "Working tree is clean."
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
        return GitHubOperationResult.Ok("Commit created locally.", [commitResult.Output]);
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

        var branchCheck = await RunGitAsync(config.LocalPath, ["show-ref", "--verify", $"refs/heads/{config.Branch}"], cancellationToken);
        if (!branchCheck.Succeeded)
        {
            return await FailAndLogAsync(
                GitHubOperationStatus.InvalidBranch,
                $"Branch '{config.Branch}' does not exist locally.",
                cancellationToken,
                [branchCheck.Error]);
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
        var pushArgs = new[]
        {
            "-c",
            $"http.extraHeader=AUTHORIZATION: basic {header}",
            "push",
            "origin",
            config.Branch
        };

        var push = await RunGitAsync(config.LocalPath, pushArgs, cancellationToken);
        if (!push.Succeeded)
        {
            var mapped = MapStatusFromGitText(push.Error + Environment.NewLine + push.Output);
            return await FailAndLogAsync(mapped, "Push failed.", cancellationToken, [push.Error, push.Output]);
        }

        await AddActivityAsync("Information", "GitHub", $"Pushed branch '{config.Branch}' to origin.", cancellationToken);
        _logger.Information($"Git push completed for branch '{config.Branch}'.");
        return GitHubOperationResult.Ok("Push completed successfully.", [push.Output]);
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
            return [];
        }

        var lines = porcelain
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
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
}
