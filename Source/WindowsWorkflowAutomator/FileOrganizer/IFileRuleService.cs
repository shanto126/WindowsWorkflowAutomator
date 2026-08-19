using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.FileOrganizer;

public interface IFileRuleService
{
    Task<IReadOnlyList<FileOrganizationRule>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<FileOrganizationRule> AddAsync(FileOrganizationRule rule, CancellationToken cancellationToken = default);

    Task UpdateAsync(FileOrganizationRule rule, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
