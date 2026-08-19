using WindowsWorkflowAutomator.SocialMedia.Adapters;
using WindowsWorkflowAutomator.SocialMedia;
using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Tests;

public sealed class SocialPlatformAdapterTests
{
    [Fact]
    public async Task ComingSoonAdapter_Returns_ComingSoon()
    {
        var adapter = new ComingSoonPlatformAdapter(SocialPlatform.Instagram, "Instagram (Coming Soon)");
        var result = await adapter.ValidateConnectionAsync();
        Assert.False(result.Succeeded);
        Assert.Equal(PlatformOperationStatus.ComingSoon, result.Status);
    }

    [Fact]
    public async Task FacebookAdapter_Forwards_To_FacebookService()
    {
        // fake Facebook service
        var called = false;
        var fake = new FakeFacebookService(() => called = true);
        var adapter = new FacebookPlatformAdapter(fake, new NoopLogger());
        var r = await adapter.ValidateConnectionAsync();
        Assert.True(called);
        Assert.True(r.Succeeded);
    }

    private sealed class FakeFacebookService : IFacebookService
    {
        private readonly Action _onValidate;
        public FakeFacebookService(Action onValidate) => _onValidate = onValidate;
        public Task<PlatformOperationResult> ConnectAsync(string? accessToken = null, CancellationToken cancellationToken = default) => Task.FromResult(PlatformOperationResult.Ok("connected"));
        public Task<PlatformOperationResult> DisconnectAsync(CancellationToken cancellationToken = default) => Task.FromResult(PlatformOperationResult.Ok("disconnected"));
        public Task<PlatformOperationResult> ValidateConnectionAsync(CancellationToken cancellationToken = default)
        {
            _onValidate();
            return Task.FromResult(PlatformOperationResult.Ok("ok"));
        }
        public Task<PlatformOperationResult> CreatePostAsync(string caption, IReadOnlyList<string> images, CancellationToken cancellationToken = default) => Task.FromResult(PlatformOperationResult.Ok("posted"));
        public Task<PlatformOperationResult> UploadMediaAsync(string imagePath, CancellationToken cancellationToken = default) => Task.FromResult(PlatformOperationResult.Ok("media"));
    }

    private sealed class NoopLogger : WindowsWorkflowAutomator.Logging.IAppLogger
    {
        public void Information(string message) { }
        public void Warning(string message) { }
        public void Error(string message, Exception? exception = null) { }
    }
}