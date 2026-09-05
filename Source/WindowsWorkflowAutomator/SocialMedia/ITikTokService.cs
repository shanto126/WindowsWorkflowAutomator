using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.SocialMedia;

public interface ITikTokService
{
    Task<PlatformOperationResult> ConnectAsync(string? accessToken = null, CancellationToken cancellationToken = default);

    Task<PlatformOperationResult> DisconnectAsync(CancellationToken cancellationToken = default);

    Task<PlatformOperationResult> ValidateConnectionAsync(CancellationToken cancellationToken = default);

    Task<PlatformOperationResult> CreatePostAsync(string caption, IReadOnlyList<string> mediaPaths, CancellationToken cancellationToken = default);

    Task<PlatformOperationResult> UploadMediaAsync(string mediaPath, CancellationToken cancellationToken = default);
}
