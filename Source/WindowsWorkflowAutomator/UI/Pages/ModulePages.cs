using WindowsWorkflowAutomator.Configuration;
using WindowsWorkflowAutomator.FileOrganizer;
using WindowsWorkflowAutomator.Logging;

namespace WindowsWorkflowAutomator.UI.Pages;

// Removed unused placeholder pages (ApplicationLauncher, WebsiteLauncher, Facebook, LinkedIn, ActivityLogs)
// to keep navigation focused. Individual placeholder classes can be restored from source history if needed.

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
