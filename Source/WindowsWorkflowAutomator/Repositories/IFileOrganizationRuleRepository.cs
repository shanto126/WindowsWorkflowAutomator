using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Repositories;

public interface IFileOrganizationRuleRepository
{
    Task<FileOrganizationRule?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FileOrganizationRule>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FileOrganizationRule>> GetEnabledAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        FileOrganizationRule rule,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        FileOrganizationRule rule,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);
}