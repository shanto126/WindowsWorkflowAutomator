using WindowsWorkflowAutomator.Models;
using WindowsWorkflowAutomator.Logging;

namespace WindowsWorkflowAutomator.SocialMedia.Adapters;

public sealed class YouTubePlatformAdapter : ISocialPlatformAdapter
{
    private readonly IYouTubeService _youTube;
    private readonly IAppLogger _logger;

    public YouTubePlatformAdapter(IYouTubeService youTube, IAppLogger logger)
    {
        _youTube = youTube;
        _logger = logger;
    }

    public SocialPlatform Platform => SocialPlatform.YouTube;
    public string DisplayName => "YouTube";
    public bool IsSupported => true;

    public Task<PlatformOperationResult> ConnectAsync(string? accessToken = null, CancellationToken cancellationToken = default) =>
        _youTube.ConnectAsync(accessToken, cancellationToken);

    public Task<PlatformOperationResult> DisconnectAsync(CancellationToken cancellationToken = default) =>
        _youTube.DisconnectAsync(cancellationToken);

    public Task<PlatformOperationResult> ValidateConnectionAsync(CancellationToken cancellationToken = default) =>
        _youTube.ValidateConnectionAsync(cancellationToken);

    public Task<PlatformOperationResult> CreatePostAsync(string caption, IReadOnlyList<string> images, CancellationToken cancellationToken = default)
    {
        _logger.Information($"YouTube: CreatePostAsync invoked with {(images?.Count ?? 0)} files.");
        return _youTube.CreatePostAsync(caption, images ?? Array.Empty<string>(), cancellationToken);
    }

    public Task<PlatformOperationResult> UploadMediaAsync(string imagePath, CancellationToken cancellationToken = default) =>
        _youTube.UploadMediaAsync(imagePath, cancellationToken);
}
