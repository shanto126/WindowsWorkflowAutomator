using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.SocialMedia;

public sealed class SocialMediaService : ISocialMediaService
{
    private readonly object _sync = new();
    private readonly List<SocialPost> _queue = [];
    private readonly IAppLogger _logger;

    public SocialMediaService(IAppLogger logger)
    {
        _logger = logger;
    }

    public Task<SocialPost> CreateDraftPostAsync(SocialDraftPostRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var post = new SocialPost
        {
            Platform = request.Platform,
            CaptionMode = request.CaptionMode,
            CaptionInput = request.CaptionInput ?? string.Empty,
            ResolvedCaption = ResolveCaption(request.CaptionMode, request.CaptionInput, request.TemplateVariables),
            Status = PostQueueStatus.Draft,
            Images = request.ImagePaths
                .Select((path, index) => new PostImage { Order = index, FilePath = path })
                .ToList(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        lock (_sync)
        {
            _queue.Add(post);
        }

        _logger.Information($"Draft post created: {post.Id} ({post.Platform}, images={post.Images.Count})");
        return Task.FromResult(Clone(post));
    }

    public async Task<IReadOnlyList<SocialPost>> CreateDraftsFromFolderAsync(
        SocialFolderDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        var imagePaths = SocialPostPlanner.GetSupportedImagesFromFolder(request.FolderPath);
        var groups = SocialPostPlanner.SplitImagePaths(imagePaths, request.PostCount, request.ImagesPerPost);

        var posts = new List<SocialPost>();
        for (var i = 0; i < groups.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var variables = BuildDefaultTemplateVariables(request.Platform, i + 1, groups.Count, groups[i].Count);
            var post = await CreateDraftPostAsync(new SocialDraftPostRequest
            {
                Platform = request.Platform,
                CaptionMode = request.CaptionMode,
                CaptionInput = request.CaptionInput,
                TemplateVariables = variables,
                ImagePaths = groups[i]
            }, cancellationToken);
            posts.Add(post);
        }

        return posts;
    }

    public Task<IReadOnlyList<SocialPost>> SchedulePostAsync(
        IReadOnlyCollection<Guid> postIds,
        DateTimeOffset? scheduledForUtc = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var updated = new List<SocialPost>();
        var scheduleTime = scheduledForUtc ?? DateTimeOffset.UtcNow;

        lock (_sync)
        {
            foreach (var id in postIds)
            {
                var post = _queue.FirstOrDefault(x => x.Id == id);
                if (post is null)
                {
                    continue;
                }

                post.Status = scheduleTime > DateTimeOffset.UtcNow
                    ? PostQueueStatus.Scheduled
                    : PostQueueStatus.Pending;
                post.ScheduledForUtc = scheduleTime;
                post.ErrorMessage = null;
                post.UpdatedAtUtc = DateTimeOffset.UtcNow;
                updated.Add(Clone(post));
            }
        }

        if (updated.Count > 0)
        {
            _logger.Information($"Scheduled {updated.Count} social post(s).");
        }

        return Task.FromResult<IReadOnlyList<SocialPost>>(updated);
    }

    public Task<IReadOnlyList<SocialPost>> GetQueueAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyList<SocialPost>>(
                _queue.OrderByDescending(x => x.UpdatedAtUtc).Select(Clone).ToArray());
        }
    }

    public Task<SocialOperationResult> CancelPostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(UpdateStatus(postId, PostQueueStatus.Cancelled, null, "Post cancelled."));
    }

    public Task<SocialOperationResult> RetryPostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            var post = _queue.FirstOrDefault(x => x.Id == postId);
            if (post is null)
            {
                return Task.FromResult(SocialOperationResult.Fail("Post not found."));
            }

            if (post.Platform == SocialPlatform.LinkedIn)
            {
                return Task.FromResult(SocialOperationResult.Fail("LinkedIn publishing is coming soon."));
            }

            post.Status = PostQueueStatus.Pending;
            post.ErrorMessage = null;
            post.UpdatedAtUtc = DateTimeOffset.UtcNow;
            _logger.Information($"Post moved to retry queue: {post.Id}");
            return Task.FromResult(SocialOperationResult.Ok("Post moved back to pending queue."));
        }
    }

    public Task<SocialOperationResult> UpdateCaptionAsync(
        Guid postId,
        CaptionMode mode,
        string captionInput,
        IReadOnlyDictionary<string, string>? variables = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            var post = _queue.FirstOrDefault(x => x.Id == postId);
            if (post is null)
            {
                return Task.FromResult(SocialOperationResult.Fail("Post not found."));
            }

            post.CaptionMode = mode;
            post.CaptionInput = captionInput ?? string.Empty;
            post.ResolvedCaption = ResolveCaption(mode, captionInput, variables ?? new Dictionary<string, string>());
            post.UpdatedAtUtc = DateTimeOffset.UtcNow;
            _logger.Information($"Caption updated for post: {post.Id}");
            return Task.FromResult(SocialOperationResult.Ok("Caption updated."));
        }
    }

    public Task<SocialOperationResult> SetProcessingAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(UpdateStatus(postId, PostQueueStatus.Processing, null, "Post is processing."));
    }

    public Task<SocialOperationResult> MarkPublishedAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(UpdateStatus(postId, PostQueueStatus.Published, null, "Post published."));
    }

    public Task<SocialOperationResult> MarkFailedAsync(Guid postId, string message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(UpdateStatus(postId, PostQueueStatus.Failed, message, "Post marked as failed."));
    }

    private SocialOperationResult UpdateStatus(Guid postId, PostQueueStatus status, string? error, string okMessage)
    {
        lock (_sync)
        {
            var post = _queue.FirstOrDefault(x => x.Id == postId);
            if (post is null)
            {
                return SocialOperationResult.Fail("Post not found.");
            }

            if (post.Platform == SocialPlatform.LinkedIn
                && (status == PostQueueStatus.Pending || status == PostQueueStatus.Processing || status == PostQueueStatus.Published))
            {
                return SocialOperationResult.Fail("LinkedIn integration is currently under development.");
            }

            post.Status = status;
            post.ErrorMessage = error;
            post.UpdatedAtUtc = DateTimeOffset.UtcNow;
            return SocialOperationResult.Ok(okMessage);
        }
    }

    private static string ResolveCaption(CaptionMode mode, string? captionInput, IReadOnlyDictionary<string, string> variables)
    {
        return mode switch
        {
            CaptionMode.Template => CaptionTemplateRenderer.Render(captionInput, variables),
            CaptionMode.AiAssisted => CaptionAssistant.Generate(captionInput, variables),
            _ => captionInput ?? string.Empty
        };
    }

    private static Dictionary<string, string> BuildDefaultTemplateVariables(
        SocialPlatform platform,
        int postIndex,
        int totalPosts,
        int imageCount)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Date"] = DateTime.Now.ToString("yyyy-MM-dd"),
            ["Time"] = DateTime.Now.ToString("HH:mm"),
            ["Platform"] = platform.ToString(),
            ["Index"] = postIndex.ToString(),
            ["Total"] = totalPosts.ToString(),
            ["ImageCount"] = imageCount.ToString()
        };
    }

    private static SocialPost Clone(SocialPost source)
    {
        return new SocialPost
        {
            Id = source.Id,
            Platform = source.Platform,
            Status = source.Status,
            CaptionMode = source.CaptionMode,
            CaptionInput = source.CaptionInput,
            ResolvedCaption = source.ResolvedCaption,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc,
            ScheduledForUtc = source.ScheduledForUtc,
            ErrorMessage = source.ErrorMessage,
            Images = source.Images.Select(x => new PostImage { Order = x.Order, FilePath = x.FilePath }).ToList()
        };
    }
}
