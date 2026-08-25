using WindowsWorkflowAutomator.Configuration;
using WindowsWorkflowAutomator.FileOrganizer;
using WindowsWorkflowAutomator.Logging;

namespace WindowsWorkflowAutomator.UI.Pages;


public sealed class ApplicationLauncherPage() : PlaceholderPage(
    "Application Launcher",
    "Launch installed desktop applications from a saved list.");

public sealed class WebsiteLauncherPage() : PlaceholderPage(
    "Website Launcher",
    "Open frequently used websites in the default browser.");

public sealed class DownloadMonitorPage : FileOrganizerPage
{
    public DownloadMonitorPage(
        IFileRuleService rules,
        IFileOrganizerService organizer,
        IDownloadFolderMonitor monitor,
        IAppSettingsService settings,
        IAppLogger logger)
        : base(rules, organizer, monitor, settings, logger)
    {
    }
}

public sealed class FacebookPage() : PlaceholderPage(
    "Facebook",
    "Facebook integration is planned. This page is a placeholder only.");

public sealed class LinkedInPage() : PlaceholderPage(
    "LinkedIn — Coming Soon",
    "LinkedIn integration is marked Coming Soon and is not implemented.");

public sealed class ActivityLogsPage() : PlaceholderPage(
    "Activity Logs",
    "Application activity will be shown here after the logging UI is implemented.");
