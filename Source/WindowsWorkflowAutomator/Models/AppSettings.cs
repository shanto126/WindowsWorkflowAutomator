namespace WindowsWorkflowAutomator.Models;

public sealed class AppSettings
{
    public string Theme { get; set; } = "Light";
    public bool StartWithWindows { get; set; }
    public string LicenseTier { get; set; } = "Free";

    public string FileOrganizerWatchFolder { get; set; } = string.Empty;

    public bool FileOrganizerMonitoringEnabled { get; set; }

    public string GitHubPatProtected { get; set; } = string.Empty;

    public string FacebookAppId { get; set; } = string.Empty;

    public string FacebookPageId { get; set; } = string.Empty;

    public string FacebookAccessTokenProtected { get; set; } = string.Empty;
}
