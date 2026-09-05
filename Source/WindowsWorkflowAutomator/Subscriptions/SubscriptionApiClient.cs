using System.Net.Http.Json;

namespace WindowsWorkflowAutomator.Subscriptions;

public sealed class SubscriptionApiClient(HttpClient httpClient)
{
    public async Task<PaymentResult?> ProcessPaymentAsync(
        int userId,
        int planId,
        string paymentMethod,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/payment/process",
            new PaymentRequest(userId, planId, paymentMethod, amount),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<PaymentResult>(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminUser>> GetUsersAsync(
        CancellationToken cancellationToken = default) =>
        await GetAsync<AdminUser[]>("api/admin/users", cancellationToken) ?? [];

    public async Task<IReadOnlyList<AdminPayment>> GetPaymentsAsync(
        CancellationToken cancellationToken = default) =>
        await GetAsync<AdminPayment[]>("api/admin/payments", cancellationToken) ?? [];

    public async Task<IReadOnlyList<AdminLicense>> GetLicensesAsync(
        CancellationToken cancellationToken = default) =>
        await GetAsync<AdminLicense[]>("api/admin/licenses", cancellationToken) ?? [];

    public async Task<UserDashboard?> GetUserDashboardAsync(
        int userId,
        CancellationToken cancellationToken = default) =>
        await GetAsync<UserDashboard>($"api/user/{userId}/dashboard", cancellationToken);

    public async Task<AdminDashboard?> GetAdminDashboardAsync(
        CancellationToken cancellationToken = default) =>
        await GetAsync<AdminDashboard>("api/admin/dashboard", cancellationToken);

    private async Task<T?> GetAsync<T>(
        string endpoint,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(endpoint, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }
}

public sealed record PaymentRequest(
    int UserId,
    int PlanId,
    string PaymentMethod,
    decimal Amount);

public sealed record PaymentResult(
    bool Success,
    string LicenseKey,
    string Message);

public sealed record AdminUser(
    int Id,
    string Name,
    string Email,
    DateTimeOffset CreatedAt);

public sealed record AdminPayment(
    int Id,
    int UserId,
    string UserName,
    decimal Amount,
    string PaymentMethod,
    string TransactionId,
    string Status,
    DateTimeOffset PaymentDate);

public sealed record AdminLicense(
    int Id,
    int UserId,
    string UserName,
    string LicenseKey,
    string PlanName,
    bool IsActive,
    DateTimeOffset ExpiryDate,
    string Status);

public sealed record UserDashboard(
    int UserId,
    string UserName,
    string PlanName,
    string LicenseStatus,
    DateTimeOffset? ExpiryDate,
    IReadOnlyList<UserPayment> PaymentHistory);

public sealed record UserPayment(
    decimal Amount,
    string PaymentMethod,
    string Status,
    DateTimeOffset PaymentDate);

public sealed record AdminDashboard(
    int TotalUsers,
    int ActiveSubscriptions,
    decimal TotalRevenue,
    int ActiveLicenses);
