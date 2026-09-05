namespace WindowsWorkflowAutomator.Models;

public sealed class PostImage
{
    public int Id { get; set; }

    public Guid SocialPostId { get; set; }

    public int Order { get; set; }

    public string FilePath { get; set; } = string.Empty;

    public SocialPost? SocialPost { get; set; }
}