using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Repositories;

public interface ISocialAccountRepository
{
    Task AddAsync(
        SocialAccount account,
        CancellationToken cancellationToken = default);

    Task<SocialAccount?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SocialAccount>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<SocialAccount?> GetByPlatformAsync(
        SocialPlatform platform,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        SocialAccount account,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}