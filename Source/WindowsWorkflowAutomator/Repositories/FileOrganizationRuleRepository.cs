using Microsoft.EntityFrameworkCore;
using WindowsWorkflowAutomator.Data;
using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Repositories;

public sealed class FileOrganizationRuleRepository
    : IFileOrganizationRuleRepository
{
    private readonly AppDbContext _db;

    public FileOrganizationRuleRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<FileOrganizationRule?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _db.FileOrganizationRules
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<FileOrganizationRule>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.FileOrganizationRules
            .OrderBy(x => x.Extension)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FileOrganizationRule>> GetEnabledAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.FileOrganizationRules
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.Extension)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        FileOrganizationRule rule,
        CancellationToken cancellationToken = default)
    {
        _db.FileOrganizationRules.Add(rule);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        FileOrganizationRule rule,
        CancellationToken cancellationToken = default)
    {
        _db.FileOrganizationRules.Update(rule);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var rule = await _db.FileOrganizationRules
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (rule is null)
        {
            return;
        }

        _db.FileOrganizationRules.Remove(rule);
        await _db.SaveChangesAsync(cancellationToken);
    }
}