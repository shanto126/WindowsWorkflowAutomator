using WindowsWorkflowAutomator.Licensing;
using WindowsWorkflowAutomator.Services.FeatureGate;

namespace WindowsWorkflowAutomator.Tests;

public class FeatureGateTests
{
    private sealed class FakeLicenseService : ILicenseService
    {
        public FakeLicenseService(bool premium)
        {
            IsPremium = premium;
        }

        public bool IsPremium { get; }

        public string CurrentTier => IsPremium ? "Premium" : "Free";

        public Task<bool> ActivateAsync(string licenseKey, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> ValidateAsync(CancellationToken cancellationToken = default) => Task.FromResult(IsPremium);
        public Task DeactivateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Fact]
    public void FreeTier_Disables_PremiumFeatures()
    {
        var svc = new FeatureGateService(new FakeLicenseService(false));

        Assert.False(svc.IsEnabled(Feature.Scheduler));
        Assert.False(svc.IsEnabled(Feature.GitHubAutomation));
        Assert.True(svc.IsEnabled(Feature.FileOrganizer));
        Assert.True(svc.IsEnabled(Feature.WorkflowAutomation));
    }

    [Fact]
    public void PremiumTier_Enables_AllFeatures()
    {
        var svc = new FeatureGateService(new FakeLicenseService(true));

        foreach (Feature f in Enum.GetValues(typeof(Feature)))
        {
            Assert.True(svc.IsEnabled(f));
        }
    }
}
