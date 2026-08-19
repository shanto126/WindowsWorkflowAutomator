namespace WindowsWorkflowAutomator.Models;

public sealed class GitHubRepository
{
    public int Id { get; set; }

    public string LocalPath { get; set; } = string.Empty;

    public string RemoteUrl { get; set; } = string.Empty;

    public string Branch { get; set; } = "develop";

    public string CommitMessageTemplate { get; set; } = "chore: backup changes";

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
