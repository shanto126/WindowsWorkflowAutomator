using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.SocialMedia.Adapters;

public sealed class RedditPlatformAdapter : ISocialPlatformAdapter
{
    private readonly IRedditService _reddit;
    private readonly IAppLogger _logger;

    public RedditPlatformAdapter(IRedditService reddit, IAppLogger logger)
    {
        _reddit = reddit;
        _logger = logger;
    }

    public SocialPlatform Platform => SocialPlatform.Reddit;
    public string DisplayName => "Reddit";
    public bool IsSupported => true;

    public Task<PlatformOperationResult> ConnectAsync(string? accessToken = null, CancellationToken cancellationToken = default) =>
        _reddit.ConnectAsync(accessToken, cancellationToken);

    public Task<PlatformOperationResult> DisconnectAsync(CancellationToken cancellationToken = default) =>
        _reddit.DisconnectAsync(cancellationToken);

    public Task<PlatformOperationResult> ValidateConnectionAsync(CancellationToken cancellationToken = default) =>
        _reddit.ValidateConnectionAsync(cancellationToken);

    public Task<PlatformOperationResult> CreatePostAsync(string caption, IReadOnlyList<string> images, CancellationToken cancellationToken = default)
    {
        var title = string.IsNullOrWhiteSpace(caption) ? "Reddit post" : caption.Trim();
        _logger.Information($"Reddit: CreatePostAsync invoked with {(images?.Count ?? 0)} media item(s) and title '{title}'.");
        return _reddit.CreatePostAsync(title, caption, images ?? Array.Empty<string>(), cancellationToken);
    }

    public Task<PlatformOperationResult> UploadMediaAsync(string imagePath, CancellationToken cancellationToken = default) =>
        _reddit.UploadMediaAsync(imagePath, cancellationToken);
}
