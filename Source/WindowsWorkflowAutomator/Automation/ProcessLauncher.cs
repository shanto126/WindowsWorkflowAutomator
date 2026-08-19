using System.Diagnostics;

namespace WindowsWorkflowAutomator.Automation;

public sealed class ProcessLauncher : IProcessLauncher
{
    public void Start(string fileName, string? arguments, bool useShellExecute)
    {
        var info = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = useShellExecute
        };

        if (!string.IsNullOrWhiteSpace(arguments))
        {
            info.Arguments = arguments;
        }

        Process.Start(info);
    }
}
