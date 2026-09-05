using Microsoft.EntityFrameworkCore;
using WindowsWorkflowAutomator.LicenseServer.Data;
using WindowsWorkflowAutomator.LicenseServer.Models;

namespace WindowsWorkflowAutomator.LicenseServer.Services;

public sealed class AuthenticationService(LicenseDbContext db)
{
    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var user = await db.Users.SingleOrDefaultAsync(
            x => x.Email == email,
            cancellationToken);

        if (user is null ||
            !string.Equals(
                user.PasswordHash,
                DemoPasswordHasher.Hash(request.Password),
                StringComparison.OrdinalIgnoreCase))
        {
            return new(false, string.Empty, 0);
        }

        return new(true, user.Role, user.Id);
    }
}
