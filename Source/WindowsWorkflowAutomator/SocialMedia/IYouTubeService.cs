using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.SocialMedia;

public interface IYouTubeService
{
    Task<PlatformOperationResult> ConnectAsync(string? accessToken = null, CancellationToken cancellationToken = default);

    Task<PlatformOperationResult> DisconnectAsync(CancellationToken cancellationToken = default);

    Task<PlatformOperationResult> ValidateConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload a video and publish it. The adapter accepts the first item in "images" as the local
    /// video file path to upload. Title is taken from caption; description is optional.
    /// </summary>
    Task<PlatformOperationResult> CreatePostAsync(string title, IReadOnlyList<string> files, CancellationToken cancellationToken = default);

    /// <summary>
    /// UploadMedia for YouTube is a convenience wrapper that will accept a local file path and
    /// return an ExternalId if uploaded. The higher-level CreatePostAsync handles video metadata.
    /// </summary>
    Task<PlatformOperationResult> UploadMediaAsync(string filePath, CancellationToken cancellationToken = default);
}
