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
        (!_licenseInfo.ExpiresAtUtc.HasValue ||
         _licenseInfo.ExpiresAtUtc.Value > DateTimeOffset.UtcNow);

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

        licenseKey = licenseKey.Trim();

        if (!IsValidLicenseKeyFormat(licenseKey))
        {
            return false;
        }

        var license = new LicenseInfo
        {
            LicenseKey = licenseKey,
            Tier = "Premium",
            IsActive = true,
            ActivatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddYears(1),
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

        return IsPremium;
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

    private static bool IsValidLicenseKeyFormat(
        string licenseKey)
    {
        if (licenseKey.Length != 17)
        {
            return false;
        }

        if (!licenseKey.StartsWith(
                "WFA-PRO-",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var parts = licenseKey.Split('-');

        if (parts.Length != 4)
        {
            return false;
        }

        return parts[2].Length == 4 &&
               parts[3].Length == 4;
    }
}