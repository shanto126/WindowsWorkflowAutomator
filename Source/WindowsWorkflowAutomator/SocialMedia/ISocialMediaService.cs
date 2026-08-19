using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.SocialMedia;

public interface ISocialMediaService
{
    Task<SocialPost> CreateDraftPostAsync(SocialDraftPostRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SocialPost>> CreateDraftsFromFolderAsync(SocialFolderDraftRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SocialPost>> SchedulePostAsync(
        IReadOnlyCollection<Guid> postIds,
        DateTimeOffset? scheduledForUtc = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SocialPost>> GetQueueAsync(CancellationToken cancellationToken = default);

    Task<SocialOperationResult> CancelPostAsync(Guid postId, CancellationToken cancellationToken = default);

    Task<SocialOperationResult> RetryPostAsync(Guid postId, CancellationToken cancellationToken = default);

    Task<SocialOperationResult> UpdateCaptionAsync(
        Guid postId,
        CaptionMode mode,
        string captionInput,
        IReadOnlyDictionary<string, string>? variables = null,
        CancellationToken cancellationToken = default);

    Task<SocialOperationResult> SetProcessingAsync(Guid postId, CancellationToken cancellationToken = default);

    Task<SocialOperationResult> MarkPublishedAsync(Guid postId, CancellationToken cancellationToken = default);

    Task<SocialOperationResult> MarkFailedAsync(Guid postId, string message, CancellationToken cancellationToken = default);
}
