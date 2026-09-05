namespace WindowsWorkflowAutomator.Models;

public sealed class SocialAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public SocialPlatform Platform { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public bool IsConnected { get; set; }

    public DateTimeOffset? LastValidatedAtUtc { get; set; }
}
