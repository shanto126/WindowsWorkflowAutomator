namespace WindowsWorkflowAutomator.SocialMedia;

public sealed class PlatformOperationResult
{
    public bool Succeeded { get; init; }

    public PlatformOperationStatus Status { get; init; }

    public string Message { get; init; } = string.Empty;

    public string? ExternalId { get; init; }

    public static PlatformOperationResult Ok(string message, string? externalId = null) => new()
    {
        Succeeded = true,
        Status = PlatformOperationStatus.Success,
        Message = message,
        ExternalId = externalId
    };

    public static PlatformOperationResult Fail(PlatformOperationStatus status, string message) => new()
    {
        Succeeded = false,
        Status = status,
        Message = message
    };
}
