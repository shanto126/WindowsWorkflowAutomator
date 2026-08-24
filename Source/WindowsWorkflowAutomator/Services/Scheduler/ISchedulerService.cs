using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Services.Scheduler;

public interface ISchedulerService
{
    Task<IReadOnlyList<ScheduledTask>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ScheduledTask?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ScheduledTask> CreateAsync(ScheduledTask task, CancellationToken cancellationToken = default);
    Task UpdateAsync(ScheduledTask task, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkflowRunResult> RunNowAsync(int id, CancellationToken cancellationToken = default);
    Task<int> RunDueSchedulesAsync(CancellationToken cancellationToken = default);
    void Start();
    void Stop();
}
