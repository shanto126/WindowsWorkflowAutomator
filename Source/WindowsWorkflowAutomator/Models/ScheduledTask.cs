namespace WindowsWorkflowAutomator.Models;

public sealed class ScheduledTask
{
    public int Id { get; set; }

    public int WorkflowId { get; set; }

    public Workflow? Workflow { get; set; }

    public ScheduleType ScheduleType { get; set; } = ScheduleType.OneTime;

    /// <summary>Local date/time at which the schedule should run.</summary>
    public DateTime ScheduledAt { get; set; }

    /// <summary>Used for weekly schedules. Ignored for one-time and daily schedules.</summary>
    public DayOfWeek? WeeklyDay { get; set; }

    public bool IsEnabled { get; set; } = true;

    /// <summary>Stored in UTC. Used to prevent duplicate runs.</summary>
    public DateTime? LastRunAtUtc { get; set; }
}
