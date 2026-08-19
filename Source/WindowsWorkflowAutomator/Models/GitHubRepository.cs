namespace WindowsWorkflowAutomator.Models;

public sealed class GitHubRepository
{
    public int Id { get; set; }

    public string LocalPath { get; set; } = string.Empty;

    public string RemoteUrl { get; set; } = string.Empty;

    public string Branch { get; set; } = "develop";

    public string CommitMessageTemplate { get; set; } = "chore: backup changes";

    // New: Sync mode for smart auto-sync (Manual / SmartAutoSync / Scheduled)
    public string SyncMode { get; set; } = "Manual";

    // New: Inactivity delay in seconds to wait before auto-commit when SmartAutoSync is enabled
    public int InactivitySeconds { get; set; } = 30;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
