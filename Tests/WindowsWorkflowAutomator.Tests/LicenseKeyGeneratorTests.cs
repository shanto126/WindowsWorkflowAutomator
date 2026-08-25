using Xunit;
using WindowsWorkflowAutomator.Licensing;

namespace WindowsWorkflowAutomator.Tests;

public class LicenseKeyGeneratorTests
{
    [Fact]
    public void Generate_ReturnsKeyInExpectedFormat()
    {
        var key = LicenseKeyGenerator.Generate("PRO", DateTimeOffset.UtcNow.AddYears(1));

        Assert.NotNull(key);
        var parts = key.Split('-');
        Assert.Equal(5, parts.Length);
        Assert.Equal("WFA", parts[0]);
        Assert.Equal("PRO", parts[1]);
        Assert.Equal(4, parts[2].Length);
        Assert.Equal(4, parts[3].Length);
        Assert.Equal(4, parts[4].Length);
        Assert.All(parts, part => Assert.True(part.All(c => char.IsUpper(c) || char.IsDigit(c))));
    }

    [Fact]
    public void TryParse_ValidKey_ReturnsDecodedData()
    {
        var expiryUtc = DateTimeOffset.UtcNow.AddMonths(6);
        var key = LicenseKeyGenerator.Generate("PRO", expiryUtc);

        var parsed = LicenseKeyGenerator.TryParse(key, out var decoded);

        Assert.True(parsed);
        Assert.Equal("PRO", decoded.Plan);
        Assert.Equal(expiryUtc.ToUnixTimeSeconds(), decoded.ExpiresAtUtc.ToUnixTimeSeconds());
    }

    [Fact]
    public void Decode_TamperedKey_Throws()
    {
        var key = LicenseKeyGenerator.Generate("PRO", DateTimeOffset.UtcNow.AddYears(1));
        var tampered = key.Substring(0, key.Length - 1) + "0";

        Assert.False(LicenseKeyGenerator.TryParse(tampered, out _));
        Assert.Throws<FormatException>(() => LicenseKeyGenerator.Decode(tampered));
    }
}
