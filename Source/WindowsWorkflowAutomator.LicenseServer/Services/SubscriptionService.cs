using Microsoft.EntityFrameworkCore;
using WindowsWorkflowAutomator.LicenseServer.Data;
using WindowsWorkflowAutomator.LicenseServer.Models;

namespace WindowsWorkflowAutomator.LicenseServer.Services;

public sealed class SubscriptionService(LicenseDbContext db)
{
    public async Task<PaymentProcessResponse> ProcessPaymentAsync(
        PaymentProcessRequest request,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([request.UserId], cancellationToken);
        var plan = await db.SubscriptionPlans.FindAsync([request.PlanId], cancellationToken);

        if (user is null || plan is null)
        {
            return new(false, string.Empty, "The demo user or subscription plan was not found.");
        }

        if (request.Amount != plan.Price)
        {
            return new(false, string.Empty, "The payment amount does not match the selected plan.");
        }

        var now = DateTimeOffset.UtcNow;
        var expiry = now.AddDays(plan.DurationDays);
        var transactionId = $"DEMO-{Guid.NewGuid():N}".ToUpperInvariant();
        var licenseKey = DemoLicenseKeyGenerator.Generate(plan.PlanName, expiry);

        db.Payments.Add(new Payment
        {
            UserId = user.Id,
            Amount = request.Amount,
            PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod)
                ? "Demo"
                : request.PaymentMethod.Trim(),
            TransactionId = transactionId,
            Status = "Success",
            PaymentDate = now
        });
        db.Subscriptions.Add(new Subscription
        {
            UserId = user.Id,
            PlanId = plan.Id,
            StartDate = now,
            EndDate = expiry,
            Status = "Active"
        });
        db.Licenses.Add(new License
        {
            UserId = user.Id,
            LicenseKey = licenseKey,
            PlanName = plan.PlanName,
            IsActive = true,
            ExpiryDate = expiry
        });

        await db.SaveChangesAsync(cancellationToken);
        return new(true, licenseKey, "Subscription activated");
    }
}
