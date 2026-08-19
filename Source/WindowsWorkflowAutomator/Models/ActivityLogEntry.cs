namespace WindowsWorkflowAutomator.Models;

public sealed class ActivityLogEntry
{
    public int Id { get; set; }
    public DateTimeOffset TimestampUtc { get; set; }
    public string Level { get; set; } = "Information";
    public string Category { get; set; } = "General";
    public string Message { get; set; } = string.Empty;
}
