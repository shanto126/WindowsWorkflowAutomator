using Microsoft.AspNetCore.Mvc;
using WindowsWorkflowAutomator.LicenseServer.Models;
using WindowsWorkflowAutomator.LicenseServer.Services;

namespace WindowsWorkflowAutomator.LicenseServer.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthenticationService authenticationService)
    : ControllerBase
{
    [HttpPost("login")]
    public Task<LoginResponse> Login(
        LoginRequest request,
        CancellationToken cancellationToken) =>
        authenticationService.LoginAsync(request, cancellationToken);
}
