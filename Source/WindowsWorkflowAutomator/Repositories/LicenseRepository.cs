using Microsoft.EntityFrameworkCore;
using WindowsWorkflowAutomator.Data;
using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Repositories;

public sealed class LicenseRepository : ILicenseRepository
{
    private readonly AppDbContext _db;

    public LicenseRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<LicenseInfo?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.LicenseInfos
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveAsync(
        LicenseInfo license,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.LicenseInfos
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            _db.LicenseInfos.Add(license);
        }
        else
        {
            existing.LicenseKey = license.LicenseKey;
            existing.Tier = license.Tier;
            existing.IsActive = license.IsActive;
            existing.ActivatedAtUtc = license.ActivatedAtUtc;
            existing.ExpiresAtUtc = license.ExpiresAtUtc;
            existing.DeviceCount = license.DeviceCount;
            existing.DeviceLimit = license.DeviceLimit;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.LicenseInfos
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            return;
        }

        _db.LicenseInfos.Remove(existing);

        await _db.SaveChangesAsync(cancellationToken);
    }
}