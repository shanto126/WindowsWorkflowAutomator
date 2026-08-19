using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.SocialMedia;

public sealed class SocialFolderDraftRequest
{
    public string FolderPath { get; init; } = string.Empty;

    public SocialPlatform Platform { get; init; }

    public int PostCount { get; init; }

    public int ImagesPerPost { get; init; }

    public CaptionMode CaptionMode { get; init; }

    public string CaptionInput { get; init; } = string.Empty;
}
