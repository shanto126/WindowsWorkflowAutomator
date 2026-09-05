using Microsoft.EntityFrameworkCore;
using WindowsWorkflowAutomator.Data;
using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Repositories;

public sealed class SocialPostRepository : ISocialPostRepository
{
    private readonly AppDbContext _db;

    public SocialPostRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(
        SocialPost post,
        CancellationToken cancellationToken = default)
    {
        _db.SocialPosts.Add(post);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<SocialPost?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _db.SocialPosts
            .Include(x => x.Images)
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<SocialPost>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.SocialPosts
            .Include(x => x.Images)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SocialPost>> GetByStatusAsync(
        PostQueueStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _db.SocialPosts
            .Include(x => x.Images)
            .Where(x => x.Status == status)
            .OrderBy(x => x.ScheduledForUtc)
            .ThenBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        SocialPost post,
        CancellationToken cancellationToken = default)
    {
        post.UpdatedAtUtc = DateTimeOffset.UtcNow;

        _db.SocialPosts.Update(post);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var post = await _db.SocialPosts
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (post is null)
        {
            return;
        }

        _db.SocialPosts.Remove(post);

        await _db.SaveChangesAsync(cancellationToken);
    }
}