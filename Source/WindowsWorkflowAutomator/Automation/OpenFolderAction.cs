using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Automation;

public sealed class OpenFolderAction : IWorkflowAction
{
    private readonly IProcessLauncher _launcher;

    public OpenFolderAction(IProcessLauncher launcher)
    {
        _launcher = launcher;
    }

    public WorkflowActionType ActionType => WorkflowActionType.OpenFolder;

    public Task ExecuteAsync(WorkflowAction step, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = step.Target?.Trim() ?? string.Empty;
        if (path.Length == 0)
        {
            throw new WorkflowActionException("Folder path is empty.");
        }

        if (!Directory.Exists(path))
        {
            throw new WorkflowActionException($"Folder path is missing: {path}");
        }

        try
        {
            _launcher.Start(path, arguments: null, useShellExecute: true);
        }
        catch (Exception ex) when (ex is not WorkflowActionException)
        {
            throw new WorkflowActionException($"Could not open folder: {path}", ex);
        }

        return Task.CompletedTask;
    }
}
