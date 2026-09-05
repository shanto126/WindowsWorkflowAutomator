using Microsoft.EntityFrameworkCore;
using WindowsWorkflowAutomator.LicenseServer.Data;
using WindowsWorkflowAutomator.LicenseServer.Models;

namespace WindowsWorkflowAutomator.LicenseServer.Services;

public sealed class UserDashboardService(LicenseDbContext db)
{
    public async Task<UserDashboardResponse?> GetAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var user = await db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var subscription = await db.Subscriptions
            .AsNoTracking()
            .Include(x => x.Plan)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var license = await db.Licenses
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var payments = await db.Payments
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Id)
            .Select(x => new UserPaymentResponse(
                x.Amount,
                x.PaymentMethod,
                x.Status,
                x.PaymentDate))
            .ToListAsync(cancellationToken);

        var expiry = license?.ExpiryDate ?? subscription?.EndDate;
        var status = license is not null &&
            license.IsActive &&
            license.ExpiryDate > DateTimeOffset.UtcNow
            ? "Active"
            : "Expired";

        return new(
            user.Id,
            user.Name,
            subscription?.Plan?.PlanName ?? license?.PlanName ?? "Free",
            status,
            expiry,
            payments);
    }
}
