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

    // Instagram settings: Instagram API (Graph) typically needs an Instagram Business User ID
    // and an access token. The access token may be shared with Facebook App tokens, but
    // keeping a separate protected token allows flexibility.
    public string InstagramAccessTokenProtected { get; set; } = string.Empty;

    // Instagram Business User ID (ig-user-id) used for publishing via the Graph API.
    public string InstagramUserId { get; set; } = string.Empty;

    // YouTube settings (store access token protected)
    public string YouTubeAccessTokenProtected { get; set; } = string.Empty;
}

