using Microsoft.AspNetCore.Mvc;
using WindowsWorkflowAutomator.LicenseServer.Models;
using WindowsWorkflowAutomator.LicenseServer.Services;

namespace WindowsWorkflowAutomator.LicenseServer.Controllers;

[ApiController]
[Route("api/license")]
public sealed class LicenseController(ILicenseValidationService validationService)
    : ControllerBase
{
    [HttpPost("validate")]
    public Task<LicenseValidationResponse> Validate(
        LicenseValidationRequest request,
        CancellationToken cancellationToken)
    {
        return validationService.ValidateAsync(
            request.LicenseKey,
            cancellationToken);
    }
}