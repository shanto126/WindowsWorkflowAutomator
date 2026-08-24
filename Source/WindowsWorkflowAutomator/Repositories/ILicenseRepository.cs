using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Repositories;

public interface ILicenseRepository
{
    Task<LicenseInfo?> GetAsync(
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        LicenseInfo license,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        CancellationToken cancellationToken = default);
}