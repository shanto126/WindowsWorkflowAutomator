namespace WindowsWorkflowAutomator.Models;

public sealed class AppSettings
{
    public string Theme { get; set; } = "Light";
    public bool StartWithWindows { get; set; }
    public string LicenseTier { get; set; } = "Free";
}
