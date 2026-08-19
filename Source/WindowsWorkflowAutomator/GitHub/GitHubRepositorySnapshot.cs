namespace WindowsWorkflowAutomator.GitHub;

public sealed class GitHubRepositorySnapshot
{
    public string LocalPath { get; init; } = string.Empty;

    public string RemoteUrl { get; init; } = string.Empty;

    public string Branch { get; init; } = "develop";

    public string CommitMessageTemplate { get; init; } = "chore: backup changes";

    public bool HasPersonalAccessToken { get; init; }

        // New: Sync mode and inactivity delay for UI
        public string SyncMode { get; init; } = "Manual";
        public int InactivitySeconds { get; init; } = 30;
    }
