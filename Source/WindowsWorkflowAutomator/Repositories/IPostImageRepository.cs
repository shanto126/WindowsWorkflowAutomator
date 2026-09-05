using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Repositories;

public interface IPostImageRepository
{
    Task AddAsync(
        PostImage image,
        CancellationToken cancellationToken = default);

    Task<PostImage?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PostImage>> GetByPostIdAsync(
        Guid socialPostId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        PostImage image,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);
}