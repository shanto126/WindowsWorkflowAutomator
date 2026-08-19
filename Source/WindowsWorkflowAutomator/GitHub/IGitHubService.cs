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
        CancellationToken cancellationToken = default);

    Task<GitHubStatusResult> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<GitHubOperationResult> CommitAsync(string message, CancellationToken cancellationToken = default);

    Task<GitHubOperationResult> PushAsync(CancellationToken cancellationToken = default);
}
