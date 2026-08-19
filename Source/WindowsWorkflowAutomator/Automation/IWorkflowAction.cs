using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Automation;

public interface IWorkflowAction
{
    WorkflowActionType ActionType { get; }

    Task ExecuteAsync(WorkflowAction step, CancellationToken cancellationToken = default);
}
