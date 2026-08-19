using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WindowsWorkflowAutomator.Configuration;
using WindowsWorkflowAutomator.Data;
using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Repositories;
using WindowsWorkflowAutomator.Services;
using WindowsWorkflowAutomator.UI;
using WindowsWorkflowAutomator.UI.Navigation;
using WindowsWorkflowAutomator.UI.Pages;

namespace WindowsWorkflowAutomator;

internal static class AppComposition
{
    public static IHost BuildHost()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.Sources.Clear();
        builder.Configuration.AddJsonFile(
            Path.Combine(AppContext.BaseDirectory, "Configuration", "appsettings.json"),
            optional: true,
            reloadOnChange: false);

        var paths = new AppPaths();
        builder.Services.AddSingleton(paths);
        builder.Services.AddSingleton<IAppLogger, FileAppLogger>();
        builder.Services.AddSingleton<IAppSettingsService, AppSettingsService>();

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={paths.DatabaseFilePath}"));

        builder.Services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
        builder.Services.AddSingleton<ApplicationStartup>();
        builder.Services.AddSingleton<ModuleNavigator>();

        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<WorkflowAutomationPage>();
        builder.Services.AddTransient<ApplicationLauncherPage>();
        builder.Services.AddTransient<WebsiteLauncherPage>();
        builder.Services.AddTransient<FileOrganizerPage>();
        builder.Services.AddTransient<DownloadMonitorPage>();
        builder.Services.AddTransient<TaskSchedulerPage>();
        builder.Services.AddTransient<GitHubAutomationPage>();
        builder.Services.AddTransient<SocialMediaManagerPage>();
        builder.Services.AddTransient<FacebookPage>();
        builder.Services.AddTransient<LinkedInPage>();
        builder.Services.AddTransient<ActivityLogsPage>();
        builder.Services.AddTransient<SettingsPage>();

        builder.Services.AddSingleton<MainForm>();

        return builder.Build();
    }
}
