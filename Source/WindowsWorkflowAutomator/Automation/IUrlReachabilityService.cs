namespace WindowsWorkflowAutomator.Automation;

public interface IUrlReachabilityService
{
    Task EnsureReachableAsync(Uri uri, CancellationToken cancellationToken = default);
}
