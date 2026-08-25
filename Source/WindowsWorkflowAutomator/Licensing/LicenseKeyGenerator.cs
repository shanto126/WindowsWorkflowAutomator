using System;
using System.Linq;

namespace WindowsWorkflowAutomator.Licensing;

public static class LicenseKeyGenerator
{
    // Generate a simple GUID-based license key grouped for readability
    public static string Generate()
    {
        var g = Guid.NewGuid().ToString("N").ToUpperInvariant();
        return string.Join("-", Enumerable.Range(0, 4).Select(i => g.Substring(i * 4, 4)));
    }
}
