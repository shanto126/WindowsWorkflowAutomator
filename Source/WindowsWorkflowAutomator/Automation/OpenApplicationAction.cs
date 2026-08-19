using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Automation;

public sealed class OpenApplicationAction : IWorkflowAction
{
    private readonly IProcessLauncher _launcher;

    public OpenApplicationAction(IProcessLauncher launcher)
    {
        _launcher = launcher;
    }

    public WorkflowActionType ActionType => WorkflowActionType.OpenApplication;

    public Task ExecuteAsync(WorkflowAction step, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = step.Target?.Trim() ?? string.Empty;
        if (path.Length == 0)
        {
            throw new WorkflowActionException("Application path is empty.");
        }

        if (!File.Exists(path))
        {
            throw new WorkflowActionException($"Application path not found: {path}");
        }

        try
        {
            _launcher.Start(path, step.Arguments, useShellExecute: true);
        }
        catch (Exception ex) when (ex is not WorkflowActionException)
        {
            throw new WorkflowActionException($"Could not launch application: {path}", ex);
        }

        return Task.CompletedTask;
    }
}
