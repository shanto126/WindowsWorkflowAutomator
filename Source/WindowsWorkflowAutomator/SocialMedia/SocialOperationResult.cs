namespace WindowsWorkflowAutomator.SocialMedia;

public sealed class SocialOperationResult
{
    public bool Succeeded { get; init; }

    public string Message { get; init; } = string.Empty;

    public static SocialOperationResult Ok(string message) => new() { Succeeded = true, Message = message };

    public static SocialOperationResult Fail(string message) => new() { Succeeded = false, Message = message };
}
