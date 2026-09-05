namespace WindowsWorkflowAutomator.LicenseServer.Models;

public sealed class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class SubscriptionPlan
{
    public int Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
}

public sealed class Payment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset PaymentDate { get; set; }
    public User? User { get; set; }
}

public sealed class Subscription
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int PlanId { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public User? User { get; set; }
    public SubscriptionPlan? Plan { get; set; }
}

public sealed class License
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string LicenseKey { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset ExpiryDate { get; set; }
    public User? User { get; set; }
}
