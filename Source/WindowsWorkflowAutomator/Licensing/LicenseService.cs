using Microsoft.Extensions.DependencyInjection;
using WindowsWorkflowAutomator.Models;
using WindowsWorkflowAutomator.Repositories;

namespace WindowsWorkflowAutomator.Licensing;

public sealed class LicenseService : ILicenseService
{
    private readonly IServiceScopeFactory _scopeFactory;

    private LicenseInfo? _licenseInfo;

    public LicenseService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public bool IsPremium =>
        _licenseInfo is not null &&
        _licenseInfo.IsActive &&
        _licenseInfo.Tier.Equals(
           "Premium",
           StringComparison.OrdinalIgnoreCase) &&
        _licenseInfo.ExpiresAtUtc.HasValue &&
        _licenseInfo.ExpiresAtUtc.Value > DateTimeOffset.UtcNow &&
        LicenseKeyGenerator.TryParse(
           _licenseInfo.LicenseKey,
           out var payload) &&
        payload.ExpiresAtUtc == _licenseInfo.ExpiresAtUtc.Value;

    public string CurrentTier =>
        IsPremium ? "Premium" : "Free";

    public async Task<bool> ActivateAsync(
        string licenseKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
        {
           return false;
        }

        var trimmedKey = licenseKey.Trim();
        if (!LicenseKeyGenerator.TryParse(trimmedKey, out var payload))
        {
           return false;
        }

        if (payload.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
           return false;
        }

        var license = new LicenseInfo
        {
           LicenseKey = trimmedKey,
           Tier = "Premium",
           IsActive = true,
           ActivatedAtUtc = DateTimeOffset.UtcNow,
           ExpiresAtUtc = payload.ExpiresAtUtc,
           DeviceCount = 1,
           DeviceLimit = 1
        };

        using var scope = _scopeFactory.CreateScope();

        var repository =
           scope.ServiceProvider.GetRequiredService<ILicenseRepository>();

        await repository.SaveAsync(
           license,
           cancellationToken);

        _licenseInfo = license;

        return true;
    }

    public async Task<bool> ValidateAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();

        var repository =
           scope.ServiceProvider.GetRequiredService<ILicenseRepository>();

        var license = await repository.GetAsync(
           cancellationToken);

        _licenseInfo = license;

        if (license is null ||
           !license.IsActive ||
           !license.Tier.Equals("Premium", StringComparison.OrdinalIgnoreCase) ||
           !license.ExpiresAtUtc.HasValue ||
           license.ExpiresAtUtc.Value <= DateTimeOffset.UtcNow ||
           !LicenseKeyGenerator.TryParse(license.LicenseKey, out var payload) ||
           payload.ExpiresAtUtc != license.ExpiresAtUtc.Value)
        {
           if (license is not null && license.ExpiresAtUtc.HasValue && license.ExpiresAtUtc.Value <= DateTimeOffset.UtcNow)
           {
               await repository.DeleteAsync(cancellationToken);
               _licenseInfo = null;
           }

           return false;
        }

        return true;
    }

    public async Task DeactivateAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();

        var repository =
           scope.ServiceProvider.GetRequiredService<ILicenseRepository>();

        await repository.DeleteAsync(
           cancellationToken);

        _licenseInfo = null;
    }
}