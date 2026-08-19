using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Repositories;

public interface IActivityLogRepository
{
    Task AddAsync(ActivityLogEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActivityLogEntry>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
}
