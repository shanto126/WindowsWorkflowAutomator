namespace WindowsWorkflowAutomator.Automation;

public sealed class WorkflowActionException : Exception
{
    public WorkflowActionException(string message)
        : base(message)
    {
    }

    public WorkflowActionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
