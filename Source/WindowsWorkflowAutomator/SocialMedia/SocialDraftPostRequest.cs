using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.SocialMedia;

public sealed class SocialDraftPostRequest
{
    public SocialPlatform Platform { get; init; }

    public CaptionMode CaptionMode { get; init; } = CaptionMode.Manual;

    public string CaptionInput { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> TemplateVariables { get; init; } = new Dictionary<string, string>();

    public IReadOnlyList<string> ImagePaths { get; init; } = [];
}
