using WindowsWorkflowAutomator.Models;
using WindowsWorkflowAutomator.Logging;

namespace WindowsWorkflowAutomator.SocialMedia.Adapters;

public sealed class FacebookPlatformAdapter : ISocialPlatformAdapter
{
    private readonly IFacebookService _facebook;
    private readonly IAppLogger _logger;

    public FacebookPlatformAdapter(IFacebookService facebook, IAppLogger logger)
    {
        _facebook = facebook;
        _logger = logger;
    }

    public SocialPlatform Platform => SocialPlatform.Facebook;
    public string DisplayName => "Facebook";
    public bool IsSupported => true;

    public Task<PlatformOperationResult> ConnectAsync(string? accessToken = null, CancellationToken cancellationToken = default) =>
        _facebook.ConnectAsync(accessToken, cancellationToken);

    public Task<PlatformOperationResult> DisconnectAsync(CancellationToken cancellationToken = default) =>
        _facebook.DisconnectAsync(cancellationToken);

    public Task<PlatformOperationResult> ValidateConnectionAsync(CancellationToken cancellationToken = default) =>
        _facebook.ValidateConnectionAsync(cancellationToken);

    public Task<PlatformOperationResult> CreatePostAsync(string caption, IReadOnlyList<string> images, CancellationToken cancellationToken = default) =>
        _facebook.CreatePostAsync(caption, images, cancellationToken);

    public Task<PlatformOperationResult> UploadMediaAsync(string imagePath, CancellationToken cancellationToken = default) =>
        _facebook.UploadMediaAsync(imagePath, cancellationToken);
}