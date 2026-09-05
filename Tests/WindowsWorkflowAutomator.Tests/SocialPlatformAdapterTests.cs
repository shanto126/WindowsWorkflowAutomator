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

    [Fact]
    public async Task RedditAdapter_Returns_NotConfigured_When_Service_Is_Not_Configured()
    {
        var fake = new FakeRedditService(PlatformOperationResult.Fail(PlatformOperationStatus.NotConfigured, "Reddit integration is not configured."));
        var adapter = new RedditPlatformAdapter(fake, new NoopLogger());

        var result = await adapter.ValidateConnectionAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(PlatformOperationStatus.NotConfigured, result.Status);
        Assert.Equal("Reddit integration is not configured.", result.Message);
    }

    [Fact]
    public async Task RedditAdapter_Forwards_Failure_Message_From_Service()
    {
        var fake = new FakeRedditService(PlatformOperationResult.Fail(PlatformOperationStatus.AuthFailure, "Reddit token is invalid or expired."));
        var adapter = new RedditPlatformAdapter(fake, new NoopLogger());

        var result = await adapter.CreatePostAsync("Sample title", new[] { "https://example.com/image.png" });

        Assert.False(result.Succeeded);
        Assert.Equal(PlatformOperationStatus.AuthFailure, result.Status);
        Assert.Equal("Reddit token is invalid or expired.", result.Message);
    }

    [Fact]
    public async Task SnapchatAdapter_Returns_ComingSoon()
    {
        var adapter = new SnapchatPlatformAdapter();
        var result = await adapter.ValidateConnectionAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(PlatformOperationStatus.ComingSoon, result.Status);
        Assert.False(adapter.IsSupported);
    }

    [Fact]
    public async Task MultiPlatformOrchestrator_Continues_When_One_Platform_Fails()
    {
        var socialMedia = new FakeSocialMediaService();
        var adapters = new ISocialPlatformAdapter[]
        {
            new FakeSupportedAdapter(SocialPlatform.Facebook),
            new FailingAdapter(SocialPlatform.YouTube),
            new FakeSupportedAdapter(SocialPlatform.Reddit)
        };
        var reddit = new FakeRedditService(PlatformOperationResult.Ok("Reddit posted"));
        var orchestrator = new MultiPlatformPostOrchestrator(socialMedia, adapters, reddit, new NoopLogger());

        var request = new MultiPlatformComposeRequest(
            new[] { SocialPlatform.Facebook, SocialPlatform.YouTube, SocialPlatform.Reddit },
            Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))).FullName,
            "Launch day",
            "This is the launch caption.",
            "#launch #demo",
            1,
            1,
            new[] { "https://example.com/image.png" });

        var results = await orchestrator.PublishAsync(request);

        Assert.Equal(3, results.Count);
        Assert.Equal(2, results.Count(x => x.Succeeded));
        Assert.Contains(results, x => x.Platform == SocialPlatform.Reddit && x.Succeeded);
        Assert.Contains(results, x => x.Platform == SocialPlatform.YouTube && !x.Succeeded);
        Assert.Contains(results, x => x.Platform == SocialPlatform.Facebook && x.Succeeded);
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

    private sealed class FakeRedditService : IRedditService
    {
        private readonly PlatformOperationResult _result;

        public FakeRedditService(PlatformOperationResult result) => _result = result;

        public Task<PlatformOperationResult> ConnectAsync(string? accessToken = null, CancellationToken cancellationToken = default) => Task.FromResult(_result);
        public Task<PlatformOperationResult> DisconnectAsync(CancellationToken cancellationToken = default) => Task.FromResult(PlatformOperationResult.Ok("disconnected"));
        public Task<PlatformOperationResult> ValidateConnectionAsync(CancellationToken cancellationToken = default) => Task.FromResult(_result);
        public Task<PlatformOperationResult> CreatePostAsync(string title, string caption, IReadOnlyList<string> images, CancellationToken cancellationToken = default) => Task.FromResult(_result);
        public Task<PlatformOperationResult> UploadMediaAsync(string imagePath, CancellationToken cancellationToken = default) => Task.FromResult(PlatformOperationResult.Ok("media"));
    }

    private sealed class FakeSocialMediaService : ISocialMediaService
    {
        private readonly List<SocialPost> _posts = [];

        public Task<SocialPost> CreateDraftPostAsync(SocialDraftPostRequest request, CancellationToken cancellationToken = default)
        {
            var post = new SocialPost
            {
                Id = Guid.NewGuid(),
                Platform = request.Platform,
                CaptionInput = request.CaptionInput ?? string.Empty,
                ResolvedCaption = request.CaptionInput ?? string.Empty,
                Images = request.ImagePaths.Select((path, index) => new PostImage { FilePath = path, Order = index }).ToList(),
                Status = PostQueueStatus.Draft,
                CaptionMode = request.CaptionMode
            };
            _posts.Add(post);
            return Task.FromResult(post);
        }

        public Task<IReadOnlyList<SocialPost>> CreateDraftsFromFolderAsync(SocialFolderDraftRequest request, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SocialPost>>([]);
        public Task<IReadOnlyList<SocialPost>> SchedulePostAsync(IReadOnlyCollection<Guid> postIds, DateTimeOffset? scheduledForUtc = null, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SocialPost>>([]);
        public Task<IReadOnlyList<SocialPost>> GetQueueAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SocialPost>>(_posts);
        public Task<SocialOperationResult> CancelPostAsync(Guid postId, CancellationToken cancellationToken = default) => Task.FromResult(SocialOperationResult.Ok("cancelled"));
        public Task<SocialOperationResult> RetryPostAsync(Guid postId, CancellationToken cancellationToken = default) => Task.FromResult(SocialOperationResult.Ok("retried"));
        public Task<SocialOperationResult> UpdateCaptionAsync(Guid postId, CaptionMode mode, string captionInput, IReadOnlyDictionary<string, string>? variables = null, CancellationToken cancellationToken = default) => Task.FromResult(SocialOperationResult.Ok("updated"));
        public Task<SocialOperationResult> SetProcessingAsync(Guid postId, CancellationToken cancellationToken = default) => Task.FromResult(SocialOperationResult.Ok("processing"));
        public Task<SocialOperationResult> MarkPublishedAsync(Guid postId, CancellationToken cancellationToken = default) => Task.FromResult(SocialOperationResult.Ok("published"));
        public Task<SocialOperationResult> MarkFailedAsync(Guid postId, string message, CancellationToken cancellationToken = default) => Task.FromResult(SocialOperationResult.Ok("failed"));
    }

    private sealed class FakeSupportedAdapter : ISocialPlatformAdapter
    {
        public FakeSupportedAdapter(SocialPlatform platform) => Platform = platform;

        public SocialPlatform Platform { get; }
        public string DisplayName => Platform.ToString();
        public bool IsSupported => true;

        public Task<PlatformOperationResult> ConnectAsync(string? accessToken = null, CancellationToken cancellationToken = default) => Task.FromResult(PlatformOperationResult.Ok("connected"));
        public Task<PlatformOperationResult> DisconnectAsync(CancellationToken cancellationToken = default) => Task.FromResult(PlatformOperationResult.Ok("disconnected"));
        public Task<PlatformOperationResult> ValidateConnectionAsync(CancellationToken cancellationToken = default) => Task.FromResult(PlatformOperationResult.Ok("ok"));
        public Task<PlatformOperationResult> CreatePostAsync(string caption, IReadOnlyList<string> images, CancellationToken cancellationToken = default) => Task.FromResult(PlatformOperationResult.Ok($"{Platform} posted"));
        public Task<PlatformOperationResult> UploadMediaAsync(string imagePath, CancellationToken cancellationToken = default) => Task.FromResult(PlatformOperationResult.Ok("media"));
    }

    private sealed class FailingAdapter : ISocialPlatformAdapter
    {
        public FailingAdapter(SocialPlatform platform) => Platform = platform;

        public SocialPlatform Platform { get; }
        public string DisplayName => Platform.ToString();
        public bool IsSupported => true;

        public Task<PlatformOperationResult> ConnectAsync(string? accessToken = null, CancellationToken cancellationToken = default) => Task.FromResult(PlatformOperationResult.Fail(PlatformOperationStatus.Error, "platform failed"));
        public Task<PlatformOperationResult> DisconnectAsync(CancellationToken cancellationToken = default) => Task.FromResult(PlatformOperationResult.Ok("disconnected"));
        public Task<PlatformOperationResult> ValidateConnectionAsync(CancellationToken cancellationToken = default) => Task.FromResult(PlatformOperationResult.Fail(PlatformOperationStatus.Error, "platform validation failed"));
        public Task<PlatformOperationResult> CreatePostAsync(string caption, IReadOnlyList<string> images, CancellationToken cancellationToken = default) => Task.FromResult(PlatformOperationResult.Fail(PlatformOperationStatus.Error, "platform failed"));
        public Task<PlatformOperationResult> UploadMediaAsync(string imagePath, CancellationToken cancellationToken = default) => Task.FromResult(PlatformOperationResult.Fail(PlatformOperationStatus.Error, "platform failed"));
    }

    private sealed class NoopLogger : WindowsWorkflowAutomator.Logging.IAppLogger
    {
        public void Information(string message) { }
        public void Warning(string message) { }
        public void Error(string message, Exception? exception = null) { }
    }
}