using Microsoft.Extensions.DependencyInjection;
using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Services;
using WindowsWorkflowAutomator.UI;

namespace WindowsWorkflowAutomator;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var host = AppComposition.BuildHost();
        var logger = host.Services.GetRequiredService<IAppLogger>();

        Application.ThreadException += (_, e) =>
            logger.Error("Unhandled UI exception.", e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            logger.Error("Unhandled domain exception.", e.ExceptionObject as Exception);

        try
        {
            host.Services.GetRequiredService<ApplicationStartup>().Initialize();
            Application.Run(host.Services.GetRequiredService<MainForm>());
        }
        catch (Exception ex)
        {
            logger.Error("Application failed to start.", ex);
            MessageBox.Show(
                "The application could not start. See the log file for details.",
                "Windows Workflow Automator",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
