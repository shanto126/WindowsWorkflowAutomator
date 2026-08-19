using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Services.Automation;

public interface IWorkflowService
{
    Task<IReadOnlyList<Workflow>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Workflow?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Workflow> CreateAsync(Workflow workflow, CancellationToken cancellationToken = default);

    Task UpdateAsync(Workflow workflow, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task SetEnabledAsync(int id, bool isEnabled, CancellationToken cancellationToken = default);

    Task<WorkflowRunResult> RunAsync(int id, CancellationToken cancellationToken = default);
}
