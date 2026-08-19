using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Automation;

public sealed class OpenWebsiteAction : IWorkflowAction
{
    private readonly IProcessLauncher _launcher;
    private readonly IUrlReachabilityService _reachability;

    public OpenWebsiteAction(IProcessLauncher launcher, IUrlReachabilityService reachability)
    {
        _launcher = launcher;
        _reachability = reachability;
    }

    public WorkflowActionType ActionType => WorkflowActionType.OpenWebsite;

    public async Task ExecuteAsync(WorkflowAction step, CancellationToken cancellationToken = default)
    {
        var raw = step.Target?.Trim() ?? string.Empty;
        if (raw.Length == 0)
        {
            throw new WorkflowActionException("Website URL is empty.");
        }

        if (!raw.Contains("://", StringComparison.Ordinal))
        {
            raw = "https://" + raw;
        }

        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new WorkflowActionException($"Website URL is invalid: {step.Target}");
        }

        try
        {
            await _reachability.EnsureReachableAsync(uri, cancellationToken);
        }
        catch (WorkflowActionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new WorkflowActionException($"Website is unreachable: {uri}", ex);
        }

        try
        {
            _launcher.Start(uri.ToString(), arguments: null, useShellExecute: true);
        }
        catch (Exception ex) when (ex is not WorkflowActionException)
        {
            throw new WorkflowActionException($"Could not open website: {uri}", ex);
        }
    }
}
