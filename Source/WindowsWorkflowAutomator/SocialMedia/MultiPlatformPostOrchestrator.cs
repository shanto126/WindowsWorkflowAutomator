using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Models;
using WindowsWorkflowAutomator.SocialMedia.Adapters;

namespace WindowsWorkflowAutomator.SocialMedia;

public sealed class MultiPlatformPostOrchestrator
{
    private readonly ISocialMediaService _socialMedia;
    private readonly IEnumerable<ISocialPlatformAdapter> _platformAdapters;
    private readonly IRedditService _redditService;
    private readonly IAppLogger _logger;

    public MultiPlatformPostOrchestrator(
        ISocialMediaService socialMedia,
        IEnumerable<ISocialPlatformAdapter> platformAdapters,
        IRedditService redditService,
        IAppLogger logger)
    {
        _socialMedia = socialMedia;
        _platformAdapters = platformAdapters;
        _redditService = redditService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PlatformPublishSummary>> PublishAsync(
        MultiPlatformComposeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var platforms = request.Platforms
            .Where(platform => platform != SocialPlatform.LinkedIn && platform != SocialPlatform.X && platform != SocialPlatform.Snapchat)
            .Distinct()
            .ToList();

        if (platforms.Count == 0)
        {
            return [];
        }

        var results = new List<PlatformPublishSummary>();
        foreach (var platform in platforms)
        {
            results.Add(await PublishPlatformAsync(platform, request, cancellationToken));
        }

        return results;
    }

    private async Task<PlatformPublishSummary> PublishPlatformAsync(
        SocialPlatform platform,
        MultiPlatformComposeRequest request,
        CancellationToken cancellationToken)
    {
        var adapter = _platformAdapters.FirstOrDefault(x => x.Platform == platform);
        if (adapter is null)
        {
            var message = $"{platform} is not configured for publishing.";
            _logger.Warning(message);
            return new PlatformPublishSummary(platform, PlatformOperationStatus.NotConfigured, false, message);
        }

        if (!adapter.IsSupported)
        {
            var message = $"{platform} is coming soon and cannot be published.";
            _logger.Warning(message);
            return new PlatformPublishSummary(platform, PlatformOperationStatus.ComingSoon, false, message);
        }

        var imagePaths = await ResolveImagePathsAsync(request, cancellationToken);
        if (imagePaths.Count == 0 && platform != SocialPlatform.Facebook)
        {
            var message = "No supported image files were found for the selected folder.";
            _logger.Warning(message);
            return new PlatformPublishSummary(platform, PlatformOperationStatus.NotConfigured, false, message);
        }

        var caption = BuildCaptionForPlatform(platform, request.Caption, request.Hashtags);
        var title = BuildTitleForPlatform(platform, request.Title, request.Caption);

        var post = await _socialMedia.CreateDraftPostAsync(new SocialDraftPostRequest
        {
            Platform = platform,
            CaptionMode = CaptionMode.Manual,
            CaptionInput = caption,
            TemplateVariables = new Dictionary<string, string>
            {
                ["Title"] = title,
                ["Caption"] = caption,
                ["Hashtags"] = request.Hashtags,
                ["Platform"] = platform.ToString()
            },
            ImagePaths = imagePaths
        }, cancellationToken);

        await _socialMedia.SetProcessingAsync(post.Id, cancellationToken);

        PlatformOperationResult publishResult;
        if (platform == SocialPlatform.Reddit)
        {
            publishResult = await _redditService.CreatePostAsync(title, caption, imagePaths, cancellationToken);
        }
        else
        {
            publishResult = await adapter.CreatePostAsync(caption, imagePaths, cancellationToken);
        }

        if (publishResult.Succeeded)
        {
            await _socialMedia.MarkPublishedAsync(post.Id, cancellationToken);
            return new PlatformPublishSummary(platform, publishResult.Status, true, publishResult.Message, publishResult.ExternalId);
        }

        await _socialMedia.MarkFailedAsync(post.Id, publishResult.Message, cancellationToken);
        return new PlatformPublishSummary(platform, publishResult.Status, false, publishResult.Message, publishResult.ExternalId);
    }

    private static async Task<IReadOnlyList<string>> ResolveImagePathsAsync(
        MultiPlatformComposeRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ImagePaths is { Count: > 0 })
        {
            return request.ImagePaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        if (string.IsNullOrWhiteSpace(request.FolderPath) || !Directory.Exists(request.FolderPath))
        {
            return [];
        }

        var images = await SocialPostPlanner.GetSupportedMediaFromFolderAsync(request.FolderPath, cancellationToken);
        if (request.PostCount <= 1 && request.ImagesPerPost <= 1)
        {
            return images.Take(1).ToList();
        }

        var groups = SocialPostPlanner.SplitImagePaths(images, request.PostCount, request.ImagesPerPost);
        return groups.FirstOrDefault() ?? [];
    }

    private static string BuildTitleForPlatform(SocialPlatform platform, string title, string caption)
    {
        if (platform == SocialPlatform.Reddit)
        {
            var candidate = string.IsNullOrWhiteSpace(title) ? caption : title;
            return string.IsNullOrWhiteSpace(candidate) ? "Reddit post" : candidate.Trim();
        }

        return string.IsNullOrWhiteSpace(title) ? caption.Trim() : title.Trim();
    }

    private static string BuildCaptionForPlatform(SocialPlatform platform, string caption, string hashtags)
    {
        var text = caption?.Trim() ?? string.Empty;
        var formattedTags = FormatHashtags(hashtags);

        if (string.IsNullOrWhiteSpace(formattedTags))
        {
            return text;
        }

        return string.IsNullOrWhiteSpace(text)
            ? formattedTags
            : $"{text} {formattedTags}";
    }

    private static string FormatHashtags(string hashtags)
    {
        if (string.IsNullOrWhiteSpace(hashtags))
        {
            return string.Empty;
        }

        var values = hashtags
            .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Length > 0 && value[0] == '#' ? value : $"#{value.TrimStart('#')}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return values.Count == 0 ? string.Empty : string.Join(" ", values);
    }
}

public sealed record MultiPlatformComposeRequest(
    IReadOnlyCollection<SocialPlatform> Platforms,
    string FolderPath,
    string Title,
    string Caption,
    string Hashtags,
    int PostCount = 1,
    int ImagesPerPost = 1,
    IReadOnlyList<string>? ImagePaths = null);

public sealed record PlatformPublishSummary(
    SocialPlatform Platform,
    PlatformOperationStatus Status,
    bool Succeeded,
    string Message,
    string? ExternalId = null);
