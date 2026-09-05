using Microsoft.AspNetCore.Mvc;
using WindowsWorkflowAutomator.LicenseServer.Models;
using WindowsWorkflowAutomator.LicenseServer.Services;

namespace WindowsWorkflowAutomator.LicenseServer.Controllers;

[ApiController]
[Route("api/payment")]
public sealed class PaymentController(SubscriptionService subscriptionService)
    : ControllerBase
{
    [HttpPost("process")]
    public Task<PaymentProcessResponse> Process(
        PaymentProcessRequest request,
        CancellationToken cancellationToken)
    {
        return subscriptionService.ProcessPaymentAsync(request, cancellationToken);
    }
}
