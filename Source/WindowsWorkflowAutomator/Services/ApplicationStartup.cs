using Microsoft.Extensions.DependencyInjection;
using WindowsWorkflowAutomator.Configuration;
using WindowsWorkflowAutomator.Data;
using WindowsWorkflowAutomator.Licensing;
using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Services.Scheduler;

namespace WindowsWorkflowAutomator.Services;

public sealed class ApplicationStartup
{
    private readonly AppPaths _paths;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAppLogger _logger;

    public ApplicationStartup(
        AppPaths paths,
        IServiceScopeFactory scopeFactory,
        IAppLogger logger)
    {
        _paths = paths;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        _paths.EnsureCreated();

        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        db.EnsureSchema();

        var licenseService = scope.ServiceProvider
            .GetRequiredService<ILicenseService>();

        var featureGate = scope.ServiceProvider
            .GetRequiredService<WindowsWorkflowAutomator.Services.FeatureGate.IFeatureGateService>();

        var isValid = await licenseService.ValidateAsync(
            cancellationToken);

        if (isValid)
        {
            _logger.Information(
                "Application initialized with a valid Premium license.");
        }
        else
        {
            _logger.Information(
                "Application initialized with a Free license.");
        }

        // Start scheduler only if the feature is enabled for the current tier.
        if (featureGate.IsEnabled(WindowsWorkflowAutomator.Services.FeatureGate.Feature.Scheduler))
        {
            var scheduler = scope.ServiceProvider.GetRequiredService<ISchedulerService>();
            scheduler.Start();
            _logger.Information("Scheduler started based on feature gate.");
        }
        else
        {
            _logger.Information("Scheduler not started: feature is not enabled for current tier.");
        }
    }
}