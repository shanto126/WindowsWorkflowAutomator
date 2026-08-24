namespace WindowsWorkflowAutomator.Services.FeatureGate;

public enum Feature
{
    Scheduler,
    GitHubAutomation,
    SocialMediaPosting,
    WorkflowAutomation,
    FileOrganizer
}

public interface IFeatureGateService
{
    bool IsEnabled(Feature feature);
}
