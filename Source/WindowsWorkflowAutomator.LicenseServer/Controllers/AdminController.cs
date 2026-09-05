using Microsoft.AspNetCore.Mvc;
using WindowsWorkflowAutomator.LicenseServer.Models;
using WindowsWorkflowAutomator.LicenseServer.Services;

namespace WindowsWorkflowAutomator.LicenseServer.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController(AdminQueryService adminQueryService)
    : ControllerBase
{
    [HttpGet("dashboard")]
    public Task<AdminDashboardResponse> GetDashboard(CancellationToken cancellationToken) =>
        adminQueryService.GetDashboardAsync(cancellationToken);

    [HttpGet("users")]
    public Task<List<AdminUserResponse>> GetUsers(CancellationToken cancellationToken) =>
        adminQueryService.GetUsersAsync(cancellationToken);

    [HttpGet("payments")]
    public Task<List<AdminPaymentResponse>> GetPayments(CancellationToken cancellationToken) =>
        adminQueryService.GetPaymentsAsync(cancellationToken);

    [HttpGet("licenses")]
    public Task<List<AdminLicenseResponse>> GetLicenses(CancellationToken cancellationToken) =>
        adminQueryService.GetLicensesAsync(cancellationToken);
}
