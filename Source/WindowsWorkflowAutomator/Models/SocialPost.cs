namespace WindowsWorkflowAutomator.Models;

public sealed class SocialPost
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public SocialPlatform Platform { get; set; }

    public PostQueueStatus Status { get; set; } = PostQueueStatus.Draft;

    public CaptionMode CaptionMode { get; set; } = CaptionMode.Manual;

    public string CaptionInput { get; set; } = string.Empty;

    public string ResolvedCaption { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ScheduledForUtc { get; set; }

    public string? ErrorMessage { get; set; }

    public List<PostImage> Images { get; set; } = [];
}
