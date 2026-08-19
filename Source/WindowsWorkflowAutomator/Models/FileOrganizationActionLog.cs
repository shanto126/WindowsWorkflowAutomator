namespace WindowsWorkflowAutomator.Models;

public sealed class FileOrganizationActionLog
{
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;

    public bool Succeeded { get; init; }

    public string SourcePath { get; init; } = string.Empty;

    public string? DestinationPath { get; init; }

    public string Message { get; init; } = string.Empty;

    public override string ToString()
    {
        var local = TimestampUtc.ToLocalTime();
        var status = Succeeded ? "OK" : "SKIP";
        return $"{local:HH:mm:ss}  [{status}]  {Message}";
    }
}
