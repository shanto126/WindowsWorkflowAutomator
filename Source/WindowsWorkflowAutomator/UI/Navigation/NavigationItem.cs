namespace WindowsWorkflowAutomator.UI.Navigation;

public sealed class NavigationItem
{
    public required string Title { get; init; }
    public required Type PageType { get; init; }
    public bool ComingSoon { get; init; }
}
