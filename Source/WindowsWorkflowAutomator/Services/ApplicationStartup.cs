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
    private readonly ISchedulerService _scheduler;

    public ApplicationStartup(
        AppPaths paths,
        IServiceScopeFactory scopeFactory,
<<<<<<< HEAD
        IAppLogger logger,
        ISchedulerService scheduler)
=======
        IAppLogger logger)
>>>>>>> origin/develop
    {
        _paths = paths;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _scheduler = scheduler;
    }

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        _paths.EnsureCreated();

        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        db.EnsureSchema();

<<<<<<< HEAD
        _scheduler.Start();
        _logger.Information("Application initialized.");
=======
        var licenseService = scope.ServiceProvider
            .GetRequiredService<ILicenseService>();

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
>>>>>>> origin/develop
    }
}