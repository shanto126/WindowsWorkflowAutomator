using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.SocialMedia.Adapters;

public sealed class YouTubePlatformAdapter : ComingSoonPlatformAdapter
{
    public YouTubePlatformAdapter() : base(SocialPlatform.YouTube, "YouTube (Coming Soon)") { }
}