using WindowsWorkflowAutomator.LicenseServer.Models;

namespace WindowsWorkflowAutomator.LicenseServer.Services;

public interface ILicenseValidationService
{
    Task<LicenseValidationResponse> ValidateAsync(
        string licenseKey,
        CancellationToken cancellationToken);
}