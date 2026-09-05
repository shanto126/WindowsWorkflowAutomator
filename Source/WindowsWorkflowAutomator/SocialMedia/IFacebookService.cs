namespace WindowsWorkflowAutomator.SocialMedia;

public interface IFacebookService
{
    Task<PlatformOperationResult> ConnectAsync(string? accessToken = null, CancellationToken cancellationToken = default);

    Task<PlatformOperationResult> DisconnectAsync(CancellationToken cancellationToken = default);

    Task<PlatformOperationResult> ValidateConnectionAsync(CancellationToken cancellationToken = default);

    Task<PlatformOperationResult> CreatePostAsync(
        string caption,
        IReadOnlyList<string> images,
        CancellationToken cancellationToken = default);

    Task<PlatformOperationResult> UploadMediaAsync(string imagePath, CancellationToken cancellationToken = default);
}
