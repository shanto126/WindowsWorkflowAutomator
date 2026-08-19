using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WindowsWorkflowAutomator.Automation;
using WindowsWorkflowAutomator.Data;
using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Models;
using WindowsWorkflowAutomator.Utilities;

namespace WindowsWorkflowAutomator.Services.Automation;

public sealed class WorkflowService : IWorkflowService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WorkflowActionFactory _actionFactory;
    private readonly IAppLogger _logger;

    public WorkflowService(
        IServiceScopeFactory scopeFactory,
        WorkflowActionFactory actionFactory,
        IAppLogger logger)
    {
        _scopeFactory = scopeFactory;
        _actionFactory = actionFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Workflow>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Workflows
            .AsNoTracking()
            .Include(x => x.Actions)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Workflow?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Workflows
            .AsNoTracking()
            .Include(x => x.Actions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Workflow> CreateAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        Normalize(workflow);
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Workflows.Add(workflow);
        await db.SaveChangesAsync(cancellationToken);
        _logger.Information($"Workflow created: {workflow.Name}");
        return workflow;
    }

    public async Task UpdateAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        Normalize(workflow);
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = await db.Workflows
            .Include(x => x.Actions)
            .FirstOrDefaultAsync(x => x.Id == workflow.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Workflow {workflow.Id} was not found.");

        existing.Name = workflow.Name;
        existing.Description = workflow.Description;
        existing.IsEnabled = workflow.IsEnabled;
        db.WorkflowActions.RemoveRange(existing.Actions);
        existing.Actions = workflow.Actions
            .Select(a => new WorkflowAction
            {
                Type = a.Type,
                Target = a.Target.Trim(),
                Arguments = string.IsNullOrWhiteSpace(a.Arguments) ? null : a.Arguments.Trim(),
                Order = a.Order
            })
            .ToList();

        await db.SaveChangesAsync(cancellationToken);
        _logger.Information($"Workflow updated: {existing.Name}");
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = await db.Workflows
            .Include(x => x.Actions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (existing is null)
        {
            return;
        }

        db.Workflows.Remove(existing);
        await db.SaveChangesAsync(cancellationToken);
        _logger.Information($"Workflow deleted: {existing.Name}");
    }

    public async Task SetEnabledAsync(int id, bool isEnabled, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = await db.Workflows.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Workflow {id} was not found.");
        existing.IsEnabled = isEnabled;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<WorkflowRunResult> RunAsync(int id, CancellationToken cancellationToken = default)
    {
        var workflow = await GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Workflow {id} was not found.");

        var steps = new List<WorkflowStepResult>();
        var ordered = workflow.Actions.OrderBy(a => a.Order).ThenBy(a => a.Id).ToList();
        _logger.Information($"Running workflow '{workflow.Name}' with {ordered.Count} action(s).");

        foreach (var action in ordered)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await _actionFactory.Resolve(action.Type).ExecuteAsync(action, cancellationToken);
                var ok = new WorkflowStepResult
                {
                    Order = action.Order,
                    Type = action.Type,
                    Target = action.Target,
                    Succeeded = true,
                    Message = $"OK: {action.Type} → {action.Target}"
                };
                steps.Add(ok);
                _logger.Information(ok.Message);
            }
            catch (Exception ex)
            {
                var message = ex is WorkflowActionException
                    ? ex.Message
                    : $"Unexpected error running {action.Type}: {ex.Message}";
                _logger.Error($"Workflow '{workflow.Name}' action failed.", ex);
                steps.Add(new WorkflowStepResult
                {
                    Order = action.Order,
                    Type = action.Type,
                    Target = action.Target,
                    Succeeded = false,
                    Message = message
                });
            }
        }

        return new WorkflowRunResult
        {
            WorkflowId = workflow.Id,
            WorkflowName = workflow.Name,
            Steps = steps
        };
    }

    private static void Normalize(Workflow workflow)
    {
        Guard.NotNull(workflow, nameof(workflow));
        workflow.Name = workflow.Name?.Trim() ?? string.Empty;
        workflow.Description = workflow.Description?.Trim() ?? string.Empty;
        if (workflow.Name.Length == 0)
        {
            throw new ArgumentException("Workflow name is required.", nameof(workflow));
        }

        workflow.Actions ??= [];
        var order = 0;
        foreach (var action in workflow.Actions)
        {
            action.Target = action.Target?.Trim() ?? string.Empty;
            if (action.Target.Length == 0)
            {
                throw new ArgumentException("Each action needs a target path or URL.", nameof(workflow));
            }

            action.Arguments = string.IsNullOrWhiteSpace(action.Arguments) ? null : action.Arguments.Trim();
            action.Order = order++;
            action.Workflow = null;
        }
    }
}
