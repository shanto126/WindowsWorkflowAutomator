namespace WindowsWorkflowAutomator.UI.Pages;

public sealed class DashboardPage() : PlaceholderPage(
    "Dashboard",
    "High-level overview of workflows, recent activity, and system status.");

public sealed class WorkflowAutomationPage() : PlaceholderPage(
    "Workflow Automation",
    "Create and run automation sequences. Implementation starts in a later phase.");

public sealed class ApplicationLauncherPage() : PlaceholderPage(
    "Application Launcher",
    "Launch installed desktop applications from a saved list.");

public sealed class WebsiteLauncherPage() : PlaceholderPage(
    "Website Launcher",
    "Open frequently used websites in the default browser.");

public sealed class FileOrganizerPage() : PlaceholderPage(
    "File Organizer",
    "Sort files into folders by type, date, or custom rules.");

public sealed class DownloadMonitorPage() : PlaceholderPage(
    "Download Folder Monitor",
    "Watch the Downloads folder and apply organizer rules.");

public sealed class TaskSchedulerPage() : PlaceholderPage(
    "Task Scheduler",
    "Schedule workflows and maintenance jobs.");

public sealed class GitHubAutomationPage() : PlaceholderPage(
    "GitHub Automation",
    "Repository helpers will be added later. No GitHub API calls are made yet.");

public sealed class SocialMediaManagerPage() : PlaceholderPage(
    "Social Media Manager",
    "A shell for social posting tools. No live social APIs are connected yet.");

public sealed class FacebookPage() : PlaceholderPage(
    "Facebook",
    "Facebook integration is planned. This page is a placeholder only.");

public sealed class LinkedInPage() : PlaceholderPage(
    "LinkedIn — Coming Soon",
    "LinkedIn integration is marked Coming Soon and is not implemented.");

public sealed class ActivityLogsPage() : PlaceholderPage(
    "Activity Logs",
    "Application activity will be shown here after the logging UI is implemented.");

public sealed class SettingsPage() : PlaceholderPage(
    "Settings",
    "Application preferences, theme, and license options will live here.");
