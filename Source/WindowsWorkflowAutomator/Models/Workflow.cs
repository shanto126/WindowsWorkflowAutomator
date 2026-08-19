namespace WindowsWorkflowAutomator.Models;

public sealed class Workflow
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public List<WorkflowAction> Actions { get; set; } = [];
}
