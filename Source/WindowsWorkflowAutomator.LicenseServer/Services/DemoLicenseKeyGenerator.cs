using System.Security.Cryptography;
using System.Text;

namespace WindowsWorkflowAutomator.LicenseServer.Services;

public static class DemoLicenseKeyGenerator
{
    public static string Generate(string plan, DateTimeOffset expiryUtc)
    {
        var normalizedPlan = plan.Trim().ToUpperInvariant();
        var expirySeconds = expiryUtc.ToUnixTimeSeconds();
        var expiryCode = EncodeBase36(expirySeconds, 8);
        var signature = ComputeSignature(normalizedPlan, expirySeconds);
        return $"WFA-{normalizedPlan}-{expiryCode[..4]}-{expiryCode[4..8]}-{signature}";
    }

    private static string ComputeSignature(string plan, long expirySeconds)
    {
        using var hmac = new HMACSHA256(
            Encoding.UTF8.GetBytes("WFA-LICENSE-KEY-SECRET-V1"));
        var hash = hmac.ComputeHash(
            Encoding.UTF8.GetBytes($"{plan}|{expirySeconds}"));
        return Convert.ToHexString(hash)[..4];
    }

    private static string EncodeBase36(long value, int minimumLength)
    {
        const string alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var characters = new Stack<char>();
        var current = value;

        while (current > 0)
        {
            characters.Push(alphabet[(int)(current % 36)]);
            current /= 36;
        }

        var encoded = new string(characters.ToArray());
        return encoded.PadLeft(minimumLength, '0');
    }
}
