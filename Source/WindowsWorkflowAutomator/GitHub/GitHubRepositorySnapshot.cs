namespace WindowsWorkflowAutomator.GitHub;

public sealed class GitHubRepositorySnapshot
{
    public string LocalPath { get; init; } = string.Empty;

    public string RemoteUrl { get; init; } = string.Empty;

    public string Branch { get; init; } = "develop";

    public string CommitMessageTemplate { get; init; } = "chore: backup changes";

    public bool HasPersonalAccessToken { get; init; }
}
