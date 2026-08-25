using System.Linq;
using Xunit;
using WindowsWorkflowAutomator.Licensing;

namespace WindowsWorkflowAutomator.Tests;

public class LicenseKeyGeneratorTests
{
    [Fact]
    public void Generate_ReturnsKeyInExpectedFormat()
    {
        var key = LicenseKeyGenerator.Generate();
        Assert.NotNull(key);
        Assert.Equal(4, key.Split('-').Length);
        foreach (var part in key.Split('-'))
        {
            Assert.Equal(4, part.Length);
            Assert.True(part.All(c => char.IsUpper(c) || char.IsDigit(c)));
        }
    }
}
