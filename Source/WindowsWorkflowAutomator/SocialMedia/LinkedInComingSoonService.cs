namespace WindowsWorkflowAutomator.SocialMedia;

public sealed class LinkedInComingSoonService : ILinkedInService
{
    private static PlatformOperationResult ComingSoon() => PlatformOperationResult.Fail(
        PlatformOperationStatus.ComingSoon,
        "LinkedIn integration is currently under development and will be available in a future update.");

    public Task<PlatformOperationResult> ConnectAsync(string? accessToken = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(ComingSoon());

    public Task<PlatformOperationResult> DisconnectAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(ComingSoon());

    public Task<PlatformOperationResult> ValidateConnectionAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(ComingSoon());

    public Task<PlatformOperationResult> CreatePostAsync(
        string caption,
        IReadOnlyList<string> images,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ComingSoon());

    public Task<PlatformOperationResult> UploadMediaAsync(string imagePath, CancellationToken cancellationToken = default) =>
        Task.FromResult(ComingSoon());
}
