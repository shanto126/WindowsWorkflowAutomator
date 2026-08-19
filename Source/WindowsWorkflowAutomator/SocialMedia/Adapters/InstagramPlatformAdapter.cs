using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.SocialMedia.Adapters;

public sealed class InstagramPlatformAdapter : ComingSoonPlatformAdapter
{
    public InstagramPlatformAdapter() : base(SocialPlatform.Instagram, "Instagram (Coming Soon)") { }
}