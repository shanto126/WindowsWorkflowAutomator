using Microsoft.Extensions.DependencyInjection;
using WindowsWorkflowAutomator.Logging;

namespace WindowsWorkflowAutomator.UI.Navigation;

public sealed class ModuleNavigator
{
    private readonly IServiceProvider _services;
    private readonly IAppLogger _logger;

    public ModuleNavigator(IServiceProvider services, IAppLogger logger)
    {
        _services = services;
        _logger = logger;
    }

    public void Show(Panel host, Type pageType, string title)
    {
        foreach (Control existing in host.Controls)
        {
            existing.Dispose();
        }

        host.Controls.Clear();

        var page = (Control)_services.GetRequiredService(pageType);
        page.Dock = DockStyle.Fill;
        host.Controls.Add(page);
        _logger.Information($"Opened module: {title}");
    }
}
