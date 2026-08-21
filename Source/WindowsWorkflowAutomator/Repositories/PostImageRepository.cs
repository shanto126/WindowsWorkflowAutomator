using Microsoft.EntityFrameworkCore;
using WindowsWorkflowAutomator.Data;
using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Repositories;

public sealed class PostImageRepository : IPostImageRepository
{
    private readonly AppDbContext _db;

    public PostImageRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(
        PostImage image,
        CancellationToken cancellationToken = default)
    {
        _db.PostImages.Add(image);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PostImage?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _db.PostImages
            .Include(x => x.SocialPost)
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<PostImage>> GetByPostIdAsync(
        Guid socialPostId,
        CancellationToken cancellationToken = default)
    {
        return await _db.PostImages
            .Where(x => x.SocialPostId == socialPostId)
            .OrderBy(x => x.Order)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        PostImage image,
        CancellationToken cancellationToken = default)
    {
        _db.PostImages.Update(image);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var image = await _db.PostImages
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (image is null)
        {
            return;
        }

        _db.PostImages.Remove(image);

        await _db.SaveChangesAsync(cancellationToken);
    }
}