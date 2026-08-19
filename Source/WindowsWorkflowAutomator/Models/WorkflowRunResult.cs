namespace WindowsWorkflowAutomator.Models;

public sealed class WorkflowStepResult
{
    public int Order { get; init; }

    public WorkflowActionType Type { get; init; }

    public string Target { get; init; } = string.Empty;

    public bool Succeeded { get; init; }

    public string Message { get; init; } = string.Empty;
}

public sealed class WorkflowRunResult
{
    public int WorkflowId { get; init; }

    public string WorkflowName { get; init; } = string.Empty;

    public bool Succeeded => Steps.Count > 0 && Steps.All(s => s.Succeeded);

    public IReadOnlyList<WorkflowStepResult> Steps { get; init; } = [];

    public string Summary
    {
        get
        {
            if (Steps.Count == 0)
            {
                return "This workflow has no actions.";
            }

            var failed = Steps.Count(s => !s.Succeeded);
            return failed == 0
                ? $"Ran {Steps.Count} action(s) successfully."
                : $"Finished with {failed} error(s) out of {Steps.Count} action(s).";
        }
    }
}
