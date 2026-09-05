namespace WindowsWorkflowAutomator.LicenseServer.Models;

public sealed class LicenseValidationRecord
{
    public int Id { get; set; }

    public string LicenseKey { get; set; } = string.Empty;

    public bool IsValid { get; set; }

    public string Tier { get; set; } = "Free";

    public string ExpiresAt { get; set; } = string.Empty;

    public DateTimeOffset ValidatedAt { get; set; }
}