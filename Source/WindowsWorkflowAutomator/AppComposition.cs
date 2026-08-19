using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WindowsWorkflowAutomator.Automation;
using WindowsWorkflowAutomator.Configuration;
using WindowsWorkflowAutomator.Data;
using WindowsWorkflowAutomator.FileOrganizer;
using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Repositories;
using WindowsWorkflowAutomator.Security;
using WindowsWorkflowAutomator.Services;
using WindowsWorkflowAutomator.Services.Automation;
using WindowsWorkflowAutomator.UI;
using WindowsWorkflowAutomator.UI.Navigation;
using WindowsWorkflowAutomator.UI.Pages;
using WindowsWorkflowAutomator.GitHub;
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
        builder.Services.AddSingleton<ISecretProtector, WindowsSecretProtector>();
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={paths.DatabaseFilePath}"));
        builder.Services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
        builder.Services.AddSingleton<IFileRuleService, FileRuleService>();
        builder.Services.AddSingleton<IFileOrganizerService, FileOrganizerService>();
        builder.Services.AddSingleton<IDownloadFolderMonitor, DownloadFolderMonitor>();
        builder.Services.AddSingleton<IProcessLauncher, ProcessLauncher>();
        builder.Services.AddSingleton<IUrlReachabilityService, HttpUrlReachabilityService>();
        builder.Services.AddSingleton<IWorkflowAction, OpenApplicationAction>();
        builder.Services.AddSingleton<IWorkflowAction, OpenWebsiteAction>();
        builder.Services.AddSingleton<IWorkflowAction, OpenFolderAction>();
        builder.Services.AddSingleton<WorkflowActionFactory>();
        builder.Services.AddSingleton<IWorkflowService, WorkflowService>();
        builder.Services.AddSingleton<IGitHubService, GitHubService>();
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