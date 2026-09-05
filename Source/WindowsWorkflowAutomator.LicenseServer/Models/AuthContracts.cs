namespace WindowsWorkflowAutomator.LicenseServer.Models;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(bool Success, string Role, int UserId);

public sealed record UserDashboardResponse(
    int UserId,
    string UserName,
    string PlanName,
    string LicenseStatus,
    DateTimeOffset? ExpiryDate,
    IReadOnlyList<UserPaymentResponse> PaymentHistory);

public sealed record UserPaymentResponse(
    decimal Amount,
    string PaymentMethod,
    string Status,
    DateTimeOffset PaymentDate);

public sealed record AdminDashboardResponse(
    int TotalUsers,
    int ActiveSubscriptions,
    decimal TotalRevenue,
    int ActiveLicenses);
