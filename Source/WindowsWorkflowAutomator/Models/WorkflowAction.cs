namespace WindowsWorkflowAutomator.Models;

public sealed class WorkflowAction
{
    public int Id { get; set; }

    public int WorkflowId { get; set; }

    public Workflow? Workflow { get; set; }

    public WorkflowActionType Type { get; set; }

    /// <summary>Executable path, website URL, or folder path.</summary>
    public string Target { get; set; } = string.Empty;

    public string? Arguments { get; set; }

    public int Order { get; set; }
}
