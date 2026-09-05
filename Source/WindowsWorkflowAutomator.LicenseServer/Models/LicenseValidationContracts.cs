namespace WindowsWorkflowAutomator.LicenseServer.Models;

public sealed record LicenseValidationRequest(string LicenseKey);

public sealed record LicenseValidationResponse(
    bool IsValid,
    string Tier,
    string ExpiresAt);