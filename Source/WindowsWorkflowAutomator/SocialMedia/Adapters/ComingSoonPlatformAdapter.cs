using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.SocialMedia.Adapters;

public class ComingSoonPlatformAdapter : ISocialPlatformAdapter
{
    private readonly SocialPlatform _platform;
    private readonly string _displayName;

    public ComingSoonPlatformAdapter(SocialPlatform platform, string displayName)
    {
        _platform = platform;
        _displayName = displayName;
    }

    public SocialPlatform Platform => _platform;
    public string DisplayName => _displayName;
    public bool IsSupported => false;

    private static PlatformOperationResult ComingSoon() => PlatformOperationResult.Fail(
        PlatformOperationStatus.ComingSoon,
        "This platform integration is coming soon and is not yet supported.");

    public Task<PlatformOperationResult> ConnectAsync(string? accessToken = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(ComingSoon());

    public Task<PlatformOperationResult> DisconnectAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(ComingSoon());

    public Task<PlatformOperationResult> ValidateConnectionAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(ComingSoon());

    public Task<PlatformOperationResult> CreatePostAsync(string caption, IReadOnlyList<string> images, CancellationToken cancellationToken = default) =>
        Task.FromResult(ComingSoon());

    public Task<PlatformOperationResult> UploadMediaAsync(string imagePath, CancellationToken cancellationToken = default) =>
        Task.FromResult(ComingSoon());
}