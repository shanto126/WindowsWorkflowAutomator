using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WindowsWorkflowAutomator.Data;
using WindowsWorkflowAutomator.Models;
using WindowsWorkflowAutomator.Utilities;

namespace WindowsWorkflowAutomator.FileOrganizer;

public sealed class FileRuleService : IFileRuleService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public FileRuleService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<IReadOnlyList<FileOrganizationRule>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.FileOrganizationRules
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<FileOrganizationRule> AddAsync(FileOrganizationRule rule, CancellationToken cancellationToken = default)
    {
        Validate(rule);
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.FileOrganizationRules.Add(rule);
        await db.SaveChangesAsync(cancellationToken);
        return rule;
    }

    public async Task UpdateAsync(FileOrganizationRule rule, CancellationToken cancellationToken = default)
    {
        Validate(rule);
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = await db.FileOrganizationRules.FirstOrDefaultAsync(x => x.Id == rule.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Rule {rule.Id} was not found.");

        existing.Extension = rule.Extension.Trim();
        existing.DestinationFolder = rule.DestinationFolder.Trim();
        existing.Action = rule.Action;
        existing.IsEnabled = rule.IsEnabled;
        existing.RenamePattern = string.IsNullOrWhiteSpace(rule.RenamePattern) ? null : rule.RenamePattern.Trim();
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = await db.FileOrganizationRules.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (existing is null)
        {
            return;
        }

        db.FileOrganizationRules.Remove(existing);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void Validate(FileOrganizationRule rule)
    {
        Guard.NotNull(rule, nameof(rule));
        if (FileOrganizationHelpers.ParseExtensions(rule.Extension).Count == 0)
        {
            throw new ArgumentException("Enter at least one file extension (for example pdf or .jpg,.png).", nameof(rule));
        }

        if (rule.Action != FileOrganizationAction.Rename && string.IsNullOrWhiteSpace(rule.DestinationFolder))
        {
            throw new ArgumentException("Choose a destination folder for move and copy rules.", nameof(rule));
        }
    }
}
