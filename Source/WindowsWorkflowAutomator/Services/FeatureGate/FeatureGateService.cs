using WindowsWorkflowAutomator.Licensing;

namespace WindowsWorkflowAutomator.Services.FeatureGate;

public sealed class FeatureGateService : IFeatureGateService
{
    private readonly ILicenseService _licenseService;

    public FeatureGateService(ILicenseService licenseService)
    {
        _licenseService = licenseService;
    }

    public bool IsEnabled(Feature feature)
    {
        // Free tier: only basic features are enabled.
        // Premium tier: all features enabled.
        if (_licenseService.IsPremium)
            return true;

        return feature switch
        {
            Feature.FileOrganizer => true,
            Feature.WorkflowAutomation => true,
            _ => false
        };
    }
}
