using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Repositories;

public interface ISocialPostRepository
{
    Task AddAsync(
        SocialPost post,
        CancellationToken cancellationToken = default);

    Task<SocialPost?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SocialPost>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SocialPost>> GetByStatusAsync(
        PostQueueStatus status,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        SocialPost post,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}