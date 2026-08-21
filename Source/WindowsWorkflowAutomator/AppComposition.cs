using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WindowsWorkflowAutomator.Automation;
using WindowsWorkflowAutomator.Configuration;
using WindowsWorkflowAutomator.Data;
using WindowsWorkflowAutomator.FileOrganizer;
using WindowsWorkflowAutomator.GitHub;
using WindowsWorkflowAutomator.Licensing;
using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Repositories;
using WindowsWorkflowAutomator.Security;
using WindowsWorkflowAutomator.Services;
using WindowsWorkflowAutomator.Services.Automation;
using WindowsWorkflowAutomator.SocialMedia;
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
        builder.Services.AddSingleton<ISecretProtector, WindowsSecretProtector>();
        builder.Services.AddSingleton<ILicenseService, LicenseService>();
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={paths.DatabaseFilePath}"));
        builder.Services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
        builder.Services.AddScoped<ILicenseRepository, LicenseRepository>();
        builder.Services.AddScoped<ISocialAccountRepository, SocialAccountRepository>();
        builder.Services.AddScoped<ISocialPostRepository, SocialPostRepository>();
        builder.Services.AddScoped<IPostImageRepository, PostImageRepository>();
        builder.Services.AddScoped<IWorkflowRepository, WorkflowRepository>();
        builder.Services.AddScoped<
        IFileOrganizationRuleRepository,
        FileOrganizationRuleRepository>();
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
        builder.Services.AddSingleton<ISocialMediaService, SocialMediaService>();
        builder.Services.AddSingleton<IFacebookService, FacebookService>();
        builder.Services.AddSingleton<ILinkedInService, LinkedInComingSoonService>();
        builder.Services.AddSingleton<IInstagramService, InstagramService>();
        builder.Services.AddSingleton<IYouTubeService, YouTubeService>();
        builder.Services.AddSingleton<ITikTokService, TikTokService>();
        builder.Services.AddSingleton<IRedditService, RedditService>();
        builder.Services.AddSingleton<IThreadsService, ThreadsService>();
        builder.Services.AddSingleton<MultiPlatformPostOrchestrator>();

        // Social platform adapters
        builder.Services.AddSingleton<WindowsWorkflowAutomator.SocialMedia.Adapters.ISocialPlatformAdapter, WindowsWorkflowAutomator.SocialMedia.Adapters.FacebookPlatformAdapter>();
        builder.Services.AddSingleton<WindowsWorkflowAutomator.SocialMedia.Adapters.ISocialPlatformAdapter, WindowsWorkflowAutomator.SocialMedia.Adapters.LinkedInPlatformAdapter>();
        builder.Services.AddSingleton<WindowsWorkflowAutomator.SocialMedia.Adapters.ISocialPlatformAdapter, WindowsWorkflowAutomator.SocialMedia.Adapters.InstagramPlatformAdapter>();
        builder.Services.AddSingleton<WindowsWorkflowAutomator.SocialMedia.Adapters.ISocialPlatformAdapter, WindowsWorkflowAutomator.SocialMedia.Adapters.XPlatformAdapter>();
        builder.Services.AddSingleton<WindowsWorkflowAutomator.SocialMedia.Adapters.ISocialPlatformAdapter, WindowsWorkflowAutomator.SocialMedia.Adapters.YouTubePlatformAdapter>();
        builder.Services.AddSingleton<WindowsWorkflowAutomator.SocialMedia.Adapters.ISocialPlatformAdapter, WindowsWorkflowAutomator.SocialMedia.Adapters.TikTokPlatformAdapter>();
        builder.Services.AddSingleton<WindowsWorkflowAutomator.SocialMedia.Adapters.ISocialPlatformAdapter, WindowsWorkflowAutomator.SocialMedia.Adapters.RedditPlatformAdapter>();
        builder.Services.AddSingleton<WindowsWorkflowAutomator.SocialMedia.Adapters.ISocialPlatformAdapter, WindowsWorkflowAutomator.SocialMedia.Adapters.ThreadsPlatformAdapter>();
        builder.Services.AddSingleton<WindowsWorkflowAutomator.SocialMedia.Adapters.ISocialPlatformAdapter, WindowsWorkflowAutomator.SocialMedia.Adapters.SnapchatPlatformAdapter>();
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