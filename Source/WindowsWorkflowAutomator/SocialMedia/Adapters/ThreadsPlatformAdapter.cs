using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.SocialMedia.Adapters;

public sealed class ThreadsPlatformAdapter : ISocialPlatformAdapter
{
    private readonly IThreadsService _threads;
    private readonly IAppLogger _logger;

    public ThreadsPlatformAdapter(IThreadsService threads, IAppLogger logger)
    {
        _threads = threads;
        _logger = logger;
    }

    public SocialPlatform Platform => SocialPlatform.Threads;
    public string DisplayName => "Threads";
    public bool IsSupported => true;

    public Task<PlatformOperationResult> ConnectAsync(string? accessToken = null, CancellationToken cancellationToken = default) =>
        _threads.ConnectAsync(accessToken, cancellationToken);

    public Task<PlatformOperationResult> DisconnectAsync(CancellationToken cancellationToken = default) =>
        _threads.DisconnectAsync(cancellationToken);

    public Task<PlatformOperationResult> ValidateConnectionAsync(CancellationToken cancellationToken = default) =>
        _threads.ValidateConnectionAsync(cancellationToken);

    public Task<PlatformOperationResult> CreatePostAsync(string caption, IReadOnlyList<string> images, CancellationToken cancellationToken = default)
    {
        _logger.Information($"Threads: CreatePostAsync invoked with {(images?.Count ?? 0)} media item(s).");
        return _threads.CreatePostAsync(caption, images ?? Array.Empty<string>(), cancellationToken);
    }

    public Task<PlatformOperationResult> UploadMediaAsync(string mediaPath, CancellationToken cancellationToken = default) =>
        _threads.UploadMediaAsync(mediaPath, cancellationToken);
}
