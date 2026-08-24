using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WindowsWorkflowAutomator.Data;
using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Services.Scheduler;

public sealed class SchedulerService : ISchedulerService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Services.Automation.IWorkflowService _workflows;
    private readonly IAppLogger _logger;
    private readonly object _gate = new();
    private Timer? _timer;
    private bool _running;

    public SchedulerService(
        IServiceScopeFactory scopeFactory,
        Services.Automation.IWorkflowService workflows,
        IAppLogger logger)
    {
        _scopeFactory = scopeFactory;
        _workflows = workflows;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ScheduledTask>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.ScheduledTasks
            .AsNoTracking()
            .Include(x => x.Workflow)
            .OrderBy(x => x.ScheduledAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<ScheduledTask?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.ScheduledTasks
            .AsNoTracking()
            .Include(x => x.Workflow)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<ScheduledTask> CreateAsync(ScheduledTask task, CancellationToken cancellationToken = default)
    {
        Validate(task);
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!await db.Workflows.AnyAsync(x => x.Id == task.WorkflowId, cancellationToken))
            throw new InvalidOperationException("The selected workflow does not exist.");

        task.Workflow = null;
        db.ScheduledTasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);
        _logger.Information($"Schedule created: #{task.Id} for workflow {task.WorkflowId}.");
        return task;
    }

    public async Task UpdateAsync(ScheduledTask task, CancellationToken cancellationToken = default)
    {
        Validate(task);
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existing = await db.ScheduledTasks.FirstOrDefaultAsync(x => x.Id == task.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Schedule {task.Id} was not found.");

        if (!await db.Workflows.AnyAsync(x => x.Id == task.WorkflowId, cancellationToken))
            throw new InvalidOperationException("The selected workflow does not exist.");

        existing.WorkflowId = task.WorkflowId;
        existing.ScheduleType = task.ScheduleType;
        existing.ScheduledAt = task.ScheduledAt;
        existing.WeeklyDay = task.WeeklyDay;
        existing.IsEnabled = task.IsEnabled;

        // Changing a schedule means it should be eligible to run again.
        existing.LastRunAtUtc = null;

        await db.SaveChangesAsync(cancellationToken);
        _logger.Information($"Schedule updated: #{task.Id}.");
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = await db.ScheduledTasks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (existing is null) return;

        db.ScheduledTasks.Remove(existing);
        await db.SaveChangesAsync(cancellationToken);
        _logger.Information($"Schedule deleted: #{id}.");
    }

    public async Task<WorkflowRunResult> RunNowAsync(int id, CancellationToken cancellationToken = default)
    {
        var task = await GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Schedule {id} was not found.");

        return await _workflows.RunAsync(task.WorkflowId, cancellationToken);
    }

    public async Task<int> RunDueSchedulesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        List<ScheduledTask> due;

        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            due = await db.ScheduledTasks
                .Include(x => x.Workflow)
                .Where(x => x.IsEnabled && x.Workflow != null && x.Workflow.IsEnabled)
                .ToListAsync(cancellationToken);
        }

        var count = 0;

        foreach (var task in due)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsDue(task, now))
                continue;

            try
            {
                _logger.Information($"Running scheduled workflow #{task.Id} for '{task.Workflow?.Name}'.");
                await _workflows.RunAsync(task.WorkflowId, cancellationToken);

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var current = await db.ScheduledTasks.FirstOrDefaultAsync(x => x.Id == task.Id, cancellationToken);
                if (current is null) continue;

                current.LastRunAtUtc = DateTime.UtcNow;
                if (current.ScheduleType == ScheduleType.OneTime)
                    current.IsEnabled = false;

                await db.SaveChangesAsync(cancellationToken);
                count++;
            }
            catch (Exception ex)
            {
                _logger.Error($"Scheduled workflow #{task.Id} failed.", ex);
            }
        }

        return count;
    }

    public void Start()
    {
        lock (_gate)
        {
            if (_running) return;
            _running = true;
            _timer = new Timer(
                async _ => await TimerTickAsync(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(30));
            _logger.Information("Task Scheduler started.");
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            if (!_running) return;
            _running = false;
            _timer?.Dispose();
            _timer = null;
            _logger.Information("Task Scheduler stopped.");
        }
    }

    private async Task TimerTickAsync()
    {
        try
        {
            if (!_running) return;
            await RunDueSchedulesAsync();
        }
        catch (Exception ex)
        {
            _logger.Error("Scheduler timer tick failed.", ex);
        }
    }

    private static bool IsDue(ScheduledTask task, DateTime now)
    {
        if (task.LastRunAtUtc.HasValue)
        {
            var lastLocal = task.LastRunAtUtc.Value.ToLocalTime();
            if (lastLocal.Date == now.Date &&
                task.ScheduleType is ScheduleType.Daily or ScheduleType.Weekly)
                return false;
        }

        if (task.ScheduleType == ScheduleType.OneTime)
            return now >= task.ScheduledAt;

        if (now.TimeOfDay < task.ScheduledAt.TimeOfDay)
            return false;

        return task.ScheduleType switch
        {
            ScheduleType.Daily => true,
            ScheduleType.Weekly => task.WeeklyDay.HasValue && task.WeeklyDay.Value == now.DayOfWeek,
            _ => false
        };
    }

    private static void Validate(ScheduledTask task)
    {
        if (task.WorkflowId <= 0)
            throw new ArgumentException("Select a workflow.");

        if (task.ScheduleType == ScheduleType.Weekly && !task.WeeklyDay.HasValue)
            throw new ArgumentException("Select a weekday for a weekly schedule.");
    }

    public void Dispose()
    {
        Stop();
    }
}
