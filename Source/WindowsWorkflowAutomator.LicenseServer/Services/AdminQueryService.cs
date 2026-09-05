using Microsoft.EntityFrameworkCore;
using WindowsWorkflowAutomator.LicenseServer.Data;
using WindowsWorkflowAutomator.LicenseServer.Models;

namespace WindowsWorkflowAutomator.LicenseServer.Services;

public sealed class AdminQueryService(LicenseDbContext db)
{
    public async Task<AdminDashboardResponse> GetDashboardAsync(
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var subscriptions = await db.Subscriptions
            .AsNoTracking()
            .Select(x => new { x.Status, x.EndDate })
            .ToListAsync(cancellationToken);
        var licenses = await db.Licenses
            .AsNoTracking()
            .Select(x => new { x.IsActive, x.ExpiryDate })
            .ToListAsync(cancellationToken);
        var successfulPayments = await db.Payments
            .AsNoTracking()
            .Where(x => x.Status == "Success")
            .Select(x => x.Amount)
            .ToListAsync(cancellationToken);
        return new(
            await db.Users.CountAsync(cancellationToken),
            subscriptions.Count(x => x.Status == "Active" && x.EndDate > now),
            successfulPayments.Sum(),
            licenses.Count(x => x.IsActive && x.ExpiryDate > now));
    }

    public Task<List<AdminUserResponse>> GetUsersAsync(CancellationToken cancellationToken) =>
        db.Users
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new AdminUserResponse(x.Id, x.Name, x.Email, x.CreatedAt))
            .ToListAsync(cancellationToken);

    public Task<List<AdminPaymentResponse>> GetPaymentsAsync(CancellationToken cancellationToken) =>
        db.Payments
            .AsNoTracking()
            .Include(x => x.User)
            .OrderByDescending(x => x.Id)
            .Select(x => new AdminPaymentResponse(
                x.Id,
                x.UserId,
                x.User!.Name,
                x.Amount,
                x.PaymentMethod,
                x.TransactionId,
                x.Status,
                x.PaymentDate))
            .ToListAsync(cancellationToken);

    public Task<List<AdminLicenseResponse>> GetLicensesAsync(CancellationToken cancellationToken) =>
        db.Licenses
            .AsNoTracking()
            .Include(x => x.User)
            .OrderByDescending(x => x.Id)
            .Select(x => new AdminLicenseResponse(
                x.Id,
                x.UserId,
                x.User!.Name,
                x.LicenseKey,
                x.PlanName,
                x.IsActive,
                x.ExpiryDate,
                x.IsActive && x.ExpiryDate > DateTimeOffset.UtcNow
                    ? "Active"
                    : "Expired"))
            .ToListAsync(cancellationToken);
}
