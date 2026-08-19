using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.UI.Pages;

internal sealed class PlatformItem
{
    public PlatformItem(SocialPlatform platform, string label, bool supported)
    {
        Platform = platform;
        Label = label;
        Supported = supported;
    }

    public SocialPlatform Platform { get; }
    public string Label { get; }
    public bool Supported { get; }

    public override string ToString() => Label + (Supported ? string.Empty : " (Coming Soon)");
}