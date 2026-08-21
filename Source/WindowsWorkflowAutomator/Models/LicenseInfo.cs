namespace WindowsWorkflowAutomator.Models;

public sealed class LicenseInfo
{
    public int Id { get; set; }

    public string LicenseKey { get; set; } = string.Empty;

    public string Tier { get; set; } = "Free";

    public bool IsActive { get; set; }

    public DateTimeOffset? ActivatedAtUtc { get; set; }

    public DateTimeOffset? ExpiresAtUtc { get; set; }

    public int DeviceCount { get; set; }

    public int DeviceLimit { get; set; } = 1;
}