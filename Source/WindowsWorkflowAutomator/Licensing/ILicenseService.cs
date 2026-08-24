using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Licensing;

public interface ILicenseService
{
    bool IsPremium { get; }

    string CurrentTier { get; }

    Task<bool> ActivateAsync(
        string licenseKey,
        CancellationToken cancellationToken = default);

    Task<bool> ValidateAsync(
        CancellationToken cancellationToken = default);

    Task DeactivateAsync(
        CancellationToken cancellationToken = default);
}