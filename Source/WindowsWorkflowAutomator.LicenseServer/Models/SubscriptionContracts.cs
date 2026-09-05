namespace WindowsWorkflowAutomator.LicenseServer.Models;

public sealed record PaymentProcessRequest(
    int UserId,
    int PlanId,
    string PaymentMethod,
    decimal Amount);

public sealed record PaymentProcessResponse(
    bool Success,
    string LicenseKey,
    string Message);

public sealed record AdminUserResponse(
    int Id,
    string Name,
    string Email,
    DateTimeOffset CreatedAt);

public sealed record AdminPaymentResponse(
    int Id,
    int UserId,
    string UserName,
    decimal Amount,
    string PaymentMethod,
    string TransactionId,
    string Status,
    DateTimeOffset PaymentDate);

public sealed record AdminLicenseResponse(
    int Id,
    int UserId,
    string UserName,
    string LicenseKey,
    string PlanName,
    bool IsActive,
    DateTimeOffset ExpiryDate,
    string Status);
