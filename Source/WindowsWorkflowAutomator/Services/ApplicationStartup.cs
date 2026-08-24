using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WindowsWorkflowAutomator.Configuration;
using WindowsWorkflowAutomator.Data;
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
        IAppLogger logger,
        ISchedulerService scheduler)
    {
        _paths = paths;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _scheduler = scheduler;
    }

    public void Initialize()
    {
        _paths.EnsureCreated();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.EnsureSchema();

        _scheduler.Start();
        _logger.Information("Application initialized.");
    }
}
