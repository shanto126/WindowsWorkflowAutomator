using WindowsWorkflowAutomator.Models;
using WindowsWorkflowAutomator.Logging;

namespace WindowsWorkflowAutomator.SocialMedia.Adapters;

public sealed class InstagramPlatformAdapter : ISocialPlatformAdapter
{
    private readonly IInstagramService _instagram;
    private readonly IAppLogger _logger;

    public InstagramPlatformAdapter(IInstagramService instagram, IAppLogger logger)
    {
        _instagram = instagram;
        _logger = logger;
    }

    public SocialPlatform Platform => SocialPlatform.Instagram;
    public string DisplayName => "Instagram";
    public bool IsSupported => true;

    public Task<PlatformOperationResult> ConnectAsync(string? accessToken = null, CancellationToken cancellationToken = default) =>
        _instagram.ConnectAsync(accessToken, cancellationToken);

    public Task<PlatformOperationResult> DisconnectAsync(CancellationToken cancellationToken = default) =>
        _instagram.DisconnectAsync(cancellationToken);

    public Task<PlatformOperationResult> ValidateConnectionAsync(CancellationToken cancellationToken = default) =>
        _instagram.ValidateConnectionAsync(cancellationToken);

    public Task<PlatformOperationResult> CreatePostAsync(string caption, IReadOnlyList<string> images, CancellationToken cancellationToken = default)
    {
        _logger.Information($"Instagram: CreatePostAsync invoked with {(images?.Count ?? 0)} images.");
        return _instagram.CreatePostAsync(caption, images, cancellationToken);
    }

    public Task<PlatformOperationResult> UploadMediaAsync(string imagePath, CancellationToken cancellationToken = default) =>
        _instagram.UploadMediaAsync(imagePath, cancellationToken);
}