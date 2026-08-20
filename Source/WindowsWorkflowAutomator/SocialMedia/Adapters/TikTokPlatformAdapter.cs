using WindowsWorkflowAutomator.Models;
using WindowsWorkflowAutomator.Logging;

namespace WindowsWorkflowAutomator.SocialMedia.Adapters;

public sealed class TikTokPlatformAdapter : ISocialPlatformAdapter
{
    private readonly ITikTokService _tikTok;
    private readonly IAppLogger _logger;

    public TikTokPlatformAdapter(ITikTokService tikTok, IAppLogger logger)
    {
        _tikTok = tikTok;
        _logger = logger;
    }

    public SocialPlatform Platform => SocialPlatform.TikTok;
    public string DisplayName => "TikTok";
    public bool IsSupported => true;

    public Task<PlatformOperationResult> ConnectAsync(string? accessToken = null, CancellationToken cancellationToken = default) =>
        _tikTok.ConnectAsync(accessToken, cancellationToken);

    public Task<PlatformOperationResult> DisconnectAsync(CancellationToken cancellationToken = default) =>
        _tikTok.DisconnectAsync(cancellationToken);

    public Task<PlatformOperationResult> ValidateConnectionAsync(CancellationToken cancellationToken = default) =>
        _tikTok.ValidateConnectionAsync(cancellationToken);

    public Task<PlatformOperationResult> CreatePostAsync(string caption, IReadOnlyList<string> images, CancellationToken cancellationToken = default)
    {
        _logger.Information($"TikTok: CreatePostAsync invoked with {(images?.Count ?? 0)} media items.");
        return _tikTok.CreatePostAsync(caption, images, cancellationToken);
    }

    public Task<PlatformOperationResult> UploadMediaAsync(string imagePath, CancellationToken cancellationToken = default) =>
        _tikTok.UploadMediaAsync(imagePath, cancellationToken);
}