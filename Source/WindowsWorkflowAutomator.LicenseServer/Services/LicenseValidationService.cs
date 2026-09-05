using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WindowsWorkflowAutomator.LicenseServer.Data;
using WindowsWorkflowAutomator.LicenseServer.Models;

namespace WindowsWorkflowAutomator.LicenseServer.Services;

public sealed class LicenseValidationService(LicenseDbContext db)
    : ILicenseValidationService
{
    public async Task<LicenseValidationResponse> ValidateAsync(
        string licenseKey,
        CancellationToken cancellationToken)
    {
        var result = await ValidateAgainstIssuedLicenseAsync(
            licenseKey,
            cancellationToken);
        var record = new LicenseValidationRecord
        {
            LicenseKey = licenseKey?.Trim() ?? string.Empty,
            IsValid = result.IsValid,
            Tier = result.Tier,
            ExpiresAt = result.ExpiresAt,
            ValidatedAt = DateTimeOffset.UtcNow
        };

        db.LicenseValidations.Add(record);
        await db.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task<LicenseValidationResponse> ValidateAgainstIssuedLicenseAsync(
        string licenseKey,
        CancellationToken cancellationToken)
    {
        var normalizedKey = licenseKey?.Trim() ?? string.Empty;
        var issuedLicense = await db.Licenses
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.LicenseKey == normalizedKey,
                cancellationToken);

        if (issuedLicense is null ||
            !issuedLicense.IsActive ||
            !issuedLicense.PlanName.Equals("Premium", StringComparison.OrdinalIgnoreCase) ||
            issuedLicense.ExpiryDate <= DateTimeOffset.UtcNow)
        {
            return new(false, "Free", string.Empty);
        }

        var parsed = Parse(normalizedKey);
        if (!parsed.IsValid ||
            !parsed.Tier.Equals("Premium", StringComparison.OrdinalIgnoreCase) ||
            !DateTimeOffset.TryParse(parsed.ExpiresAt, out var parsedExpiry) ||
                parsedExpiry.ToUnixTimeSeconds() != issuedLicense.ExpiryDate.ToUnixTimeSeconds())
        {
            return new(false, "Free", string.Empty);
        }

        return parsed;
    }

    private static LicenseValidationResponse Parse(string? licenseKey)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
        {
            return new(false, "Free", string.Empty);
        }

        var segments = licenseKey.Trim().Split('-');
        if (segments.Length != 5 ||
            !segments[0].Equals("WFA", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(segments[1]) ||
            segments[2].Length != 4 ||
            segments[3].Length != 4 ||
            segments[4].Length != 4 ||
            !TryDecodeBase36(segments[2] + segments[3], out var expirySeconds))
        {
            return new(false, "Free", string.Empty);
        }

        var expected = ComputeSignature(segments[1], expirySeconds);
        DateTimeOffset expiresAt;
        try
        {
            expiresAt = DateTimeOffset.FromUnixTimeSeconds(expirySeconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            return new(false, "Free", string.Empty);
        }
        var isValid = CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(segments[4].ToUpperInvariant()),
            Encoding.ASCII.GetBytes(expected)) &&
            expiresAt > DateTimeOffset.UtcNow;

        return new(
            isValid,
            isValid ? "Premium" : "Free",
            isValid ? expiresAt.ToString("O") : string.Empty);
    }

    private static string ComputeSignature(string plan, long expirySeconds)
    {
        using var hmac = new HMACSHA256(
            Encoding.UTF8.GetBytes("WFA-LICENSE-KEY-SECRET-V1"));
        var hash = hmac.ComputeHash(
            Encoding.UTF8.GetBytes($"{plan}|{expirySeconds}"));
        return Convert.ToHexString(hash)[..4];
    }

    private static bool TryDecodeBase36(string value, out long decoded)
    {
        decoded = 0;
        foreach (var character in value.ToUpperInvariant())
        {
            var digit = character switch
            {
                >= '0' and <= '9' => character - '0',
                >= 'A' and <= 'Z' => character - 'A' + 10,
                _ => -1
            };

            if (digit < 0)
            {
                return false;
            }

            try
            {
                decoded = checked(decoded * 36 + digit);
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        return decoded > 0;
    }
}