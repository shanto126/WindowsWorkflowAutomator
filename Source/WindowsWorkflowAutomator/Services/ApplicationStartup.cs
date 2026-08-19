using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WindowsWorkflowAutomator.Configuration;
using WindowsWorkflowAutomator.Data;
using WindowsWorkflowAutomator.Logging;

namespace WindowsWorkflowAutomator.Services;

public sealed class ApplicationStartup
{
    private readonly AppPaths _paths;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAppLogger _logger;

    public ApplicationStartup(AppPaths paths, IServiceScopeFactory scopeFactory, IAppLogger logger)
    {
        _paths = paths;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void Initialize()
    {
        _paths.EnsureCreated();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.EnsureSchema();

        _logger.Information("Application initialized.");
    }
}
