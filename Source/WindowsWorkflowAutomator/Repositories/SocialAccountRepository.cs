using Microsoft.EntityFrameworkCore;
using WindowsWorkflowAutomator.Data;
using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Repositories;

public sealed class SocialAccountRepository : ISocialAccountRepository
{
    private readonly AppDbContext _db;

    public SocialAccountRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(
        SocialAccount account,
        CancellationToken cancellationToken = default)
    {
        _db.SocialAccounts.Add(account);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<SocialAccount?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _db.SocialAccounts
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<SocialAccount>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.SocialAccounts
            .OrderBy(x => x.Platform)
            .ThenBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<SocialAccount?> GetByPlatformAsync(
        SocialPlatform platform,
        CancellationToken cancellationToken = default)
    {
        return await _db.SocialAccounts
            .FirstOrDefaultAsync(
                x => x.Platform == platform,
                cancellationToken);
    }

    public async Task UpdateAsync(
        SocialAccount account,
        CancellationToken cancellationToken = default)
    {
        _db.SocialAccounts.Update(account);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var account = await _db.SocialAccounts
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (account is null)
        {
            return;
        }

        _db.SocialAccounts.Remove(account);

        await _db.SaveChangesAsync(cancellationToken);
    }
}