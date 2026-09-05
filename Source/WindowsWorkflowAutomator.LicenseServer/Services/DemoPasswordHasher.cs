using System.Security.Cryptography;
using System.Text;

namespace WindowsWorkflowAutomator.LicenseServer.Services;

public static class DemoPasswordHasher
{
    public static string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
}
