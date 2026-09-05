using System.Security.Cryptography;
using System.Text;

namespace WindowsWorkflowAutomator.Licensing;

public static class LicenseKeyGenerator
{
    private const string AppSecret = "WFA-LICENSE-KEY-SECRET-V1";
    private const string Prefix = "WFA";
    private const int ExpiryCodeLength = 8;
    private const int SignatureCodeLength = 4;

    public static string Generate(string plan = "PRO", DateTimeOffset? expiryUtc = null)
    {
        var normalizedPlan = string.IsNullOrWhiteSpace(plan)
            ? "PRO"
            : plan.Trim();

        normalizedPlan = normalizedPlan.ToUpperInvariant();

        var expiry = expiryUtc?.ToUniversalTime() ?? DateTimeOffset.UtcNow.AddYears(1);
        var expiryUnixSeconds = expiry.ToUnixTimeSeconds();

        var expiryCode = EncodeBase36(expiryUnixSeconds, ExpiryCodeLength);
        var signatureCode = ComputeSignature(normalizedPlan, expiryUnixSeconds);

        return $"{Prefix}-{normalizedPlan}-{expiryCode.Substring(0, 4)}-{expiryCode.Substring(4, 4)}-{signatureCode}";
    }

    public static LicenseKeyData Decode(string licenseKey)
    {
        if (!TryParse(licenseKey, out var decoded))
        {
            throw new FormatException("The license key is invalid or has been tampered with.");
        }

        return decoded;
    }

    public static bool TryParse(string? licenseKey, out LicenseKeyData decoded)
    {
        decoded = default!;

        if (string.IsNullOrWhiteSpace(licenseKey))
        {
            return false;
        }

        var key = licenseKey.Trim();
        var segments = key.Split('-');

        if (segments.Length != 5)
        {
            return false;
        }

        if (!string.Equals(segments[0], Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var plan = segments[1].Trim();
        if (string.IsNullOrWhiteSpace(plan))
        {
            return false;
        }

        if (segments[2].Length != 4 ||
            segments[3].Length != 4 ||
            segments[4].Length != SignatureCodeLength)
        {
            return false;
        }

        if (!TryDecodeBase36(segments[2] + segments[3], out var expiryUnixSeconds))
        {
            return false;
        }

        if (expiryUnixSeconds <= 0)
        {
            return false;
        }

        var expectedSignature = ComputeSignature(plan, expiryUnixSeconds);
        if (!string.Equals(segments[4], expectedSignature, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        decoded = new LicenseKeyData(plan.ToUpperInvariant(), DateTimeOffset.FromUnixTimeSeconds(expiryUnixSeconds));
        return true;
    }

    private static string ComputeSignature(string plan, long expiryUnixSeconds)
    {
        var payload = $"{plan}|{expiryUnixSeconds}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(AppSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash)
            .Substring(0, SignatureCodeLength)
            .ToUpperInvariant();
    }

    private static string EncodeBase36(long value, int minimumLength)
    {
        const string alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        if (value == 0)
        {
            return new string('0', minimumLength);
        }

        var chars = new Stack<char>();
        var current = value;

        while (current > 0)
        {
            var remainder = (int)(current % 36);
            chars.Push(alphabet[remainder]);
            current /= 36;
        }

        var encoded = new string(chars.ToArray());
        if (encoded.Length < minimumLength)
        {
            encoded = new string('0', minimumLength - encoded.Length) + encoded;
        }

        return encoded;
    }

    private static bool TryDecodeBase36(string value, out long decoded)
    {
        decoded = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (var character in value.Trim().ToUpperInvariant())
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

        return true;
    }
}

public sealed record LicenseKeyData(string Plan, DateTimeOffset ExpiresAtUtc);
