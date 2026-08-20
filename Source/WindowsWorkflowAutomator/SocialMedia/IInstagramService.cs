using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.SocialMedia;

public interface IInstagramService
{
    Task<PlatformOperationResult> ConnectAsync(string? accessToken = null, CancellationToken cancellationToken = default);

    Task<PlatformOperationResult> DisconnectAsync(CancellationToken cancellationToken = default);

    Task<PlatformOperationResult> ValidateConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a post. Supported: single-image posts where the image is provided as a publicly
    /// accessible HTTP/HTTPS URL. Local file paths are not supported by the Instagram Graph API
    /// in this codepath and will return a Limited/NotSupported result with guidance.
    /// </summary>
    Task<PlatformOperationResult> CreatePostAsync(string caption, IReadOnlyList<string> images, CancellationToken cancellationToken = default);

    /// <summary>
    /// UploadMedia is only supported for public URLs (returns the same URL as ExternalId).
    /// Local file uploads are not supported here.
    /// </summary>
    Task<PlatformOperationResult> UploadMediaAsync(string imagePath, CancellationToken cancellationToken = default);
}
