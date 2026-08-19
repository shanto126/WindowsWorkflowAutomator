using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Automation;

public sealed class WorkflowActionFactory
{
    private readonly Dictionary<WorkflowActionType, IWorkflowAction> _actions;

    public WorkflowActionFactory(IEnumerable<IWorkflowAction> actions)
    {
        _actions = actions.ToDictionary(x => x.ActionType);
    }

    public IWorkflowAction Resolve(WorkflowActionType type)
    {
        if (_actions.TryGetValue(type, out var action))
        {
            return action;
        }

        throw new WorkflowActionException($"Unsupported action type: {type}");
    }
}
