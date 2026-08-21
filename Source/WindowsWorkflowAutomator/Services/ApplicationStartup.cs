using Microsoft.Extensions.DependencyInjection;
using WindowsWorkflowAutomator.Configuration;
using WindowsWorkflowAutomator.Data;
using WindowsWorkflowAutomator.Licensing;
using WindowsWorkflowAutomator.Logging;

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
    }
}