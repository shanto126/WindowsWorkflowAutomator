using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Repositories;

public interface IWorkflowRepository
{
    Task<Workflow?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Workflow>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Workflow workflow,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Workflow workflow,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);
}