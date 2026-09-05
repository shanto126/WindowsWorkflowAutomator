using Microsoft.EntityFrameworkCore;
using WindowsWorkflowAutomator.Data;
using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Repositories;

public sealed class WorkflowRepository : IWorkflowRepository
{
    private readonly AppDbContext _db;

    public WorkflowRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Workflow?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _db.Workflows
            .Include(x => x.Actions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Workflow>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Workflows
            .Include(x => x.Actions)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Workflow workflow,
        CancellationToken cancellationToken = default)
    {
        _db.Workflows.Add(workflow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Workflow workflow,
        CancellationToken cancellationToken = default)
    {
        _db.Workflows.Update(workflow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var workflow = await _db.Workflows
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (workflow is null)
        {
            return;
        }

        _db.Workflows.Remove(workflow);
        await _db.SaveChangesAsync(cancellationToken);
    }
}