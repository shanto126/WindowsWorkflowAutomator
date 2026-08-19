namespace WindowsWorkflowAutomator.GitHub;

public interface IGitHubService
{
    Task<GitHubRepositorySnapshot?> GetConfigurationAsync(CancellationToken cancellationToken = default);

    Task<GitHubOperationResult> ConfigureRepositoryAsync(
        string localPath,
        string remoteUrl,
        string branch,
        string? commitMessageTemplate = null,
        string? personalAccessToken = null,
        string? syncMode = null,
        int? inactivitySeconds = null,
        CancellationToken cancellationToken = default);

    Task<GitHubStatusResult> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<GitHubOperationResult> CommitAsync(string message, CancellationToken cancellationToken = default);

    Task<GitHubOperationResult> PushAsync(CancellationToken cancellationToken = default);

    // Expose a small activity/log helper for AutoSync
    Task LogActivityAsync(string level, string category, string message, CancellationToken cancellationToken = default);

    // Expose simple auto-sync status information
    Task<GitHubRepositorySnapshot?> GetConfigurationWithSyncAsync(CancellationToken cancellationToken = default);
}
