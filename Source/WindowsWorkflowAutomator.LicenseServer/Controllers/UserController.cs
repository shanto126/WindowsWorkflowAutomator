using Microsoft.AspNetCore.Mvc;
using WindowsWorkflowAutomator.LicenseServer.Models;
using WindowsWorkflowAutomator.LicenseServer.Services;

namespace WindowsWorkflowAutomator.LicenseServer.Controllers;

[ApiController]
[Route("api/user")]
public sealed class UserController(UserDashboardService dashboardService)
    : ControllerBase
{
    [HttpGet("{userId:int}/dashboard")]
    public async Task<ActionResult<UserDashboardResponse>> GetDashboard(
        int userId,
        CancellationToken cancellationToken)
    {
        var dashboard = await dashboardService.GetAsync(userId, cancellationToken);
        return dashboard is null ? NotFound() : Ok(dashboard);
    }
}
