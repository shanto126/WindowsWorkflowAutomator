namespace WindowsWorkflowAutomator.Automation;

public interface IProcessLauncher
{
    void Start(string fileName, string? arguments, bool useShellExecute);
}
