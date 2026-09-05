using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Authentication;
using WindowsWorkflowAutomator.UI.Navigation;
using WindowsWorkflowAutomator.UI.Pages;

namespace WindowsWorkflowAutomator.UI;

public partial class MainForm : Form
{
    private readonly ModuleNavigator _navigator;
    private readonly IAppLogger _logger;
    private readonly AppSession _session;
    private readonly List<Button> _navButtons = [];
    private Button? _activeButton;

    public MainForm(
        ModuleNavigator navigator,
        IAppLogger logger,
        AppSession session)
    {
        _navigator = navigator;
        _logger = logger;
        _session = session;
        _session.LoggedIn += OnLoggedIn;
        _session.LoggedOut += OnLoggedOut;
        InitializeComponent();
        BuildNavigation();
        ShowLogin();
        _logger.Information("Main window ready.");
    }

    private void ShowLogin()
    {
        sidebarPanel.Visible = false;
        headerTitleLabel.Text = "Login";
        _navigator.Show(contentPanel, typeof(LoginPage), "Login");
    }

    private void OnLoggedIn(object? sender, EventArgs e)
    {
        sidebarPanel.Visible = true;
        var pageType = _session.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
            ? typeof(AdminDashboardPage)
            : typeof(UserDashboardPage);
        var title = _session.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
            ? "Admin Dashboard"
            : "User Dashboard";
        headerTitleLabel.Text = title;
        _navigator.Show(contentPanel, pageType, title);
        statusLabel.Text = $"Ready  •  {title}";
    }

    private void OnLoggedOut(object? sender, EventArgs e) => ShowLogin();

    private static NavigationItem[] CreateModules() =>
    [
        new() { Title = "Dashboard", PageType = typeof(DashboardPage) },
        new() { Title = "Workflow Automation", PageType = typeof(WorkflowAutomationPage) },
        new() { Title = "File Organizer", PageType = typeof(FileOrganizerPage) },
        new() { Title = "Download Monitor", PageType = typeof(DownloadMonitorPage) },
        new() { Title = "Task Scheduler", PageType = typeof(TaskSchedulerPage) },
        new() { Title = "GitHub Automation", PageType = typeof(GitHubAutomationPage) },
        new() { Title = "Social Media Manager", PageType = typeof(SocialMediaManagerPage) },
        new() { Title = "Activity Logs", PageType = typeof(ActivityLogsPage) },
        new() { Title = "Settings", PageType = typeof(SettingsPage) },
        new() { Title = "Subscription", PageType = typeof(SubscriptionPage) },
        new() { Title = "Admin", PageType = typeof(AdminPage) }
    ];

    private void BuildNavigation()
    {
        foreach (var module in CreateModules())
        {
            var button = new Button
            {
                Text = module.ComingSoon ? $"{module.Title}  •  Soon" : module.Title,
                Tag = module,
                Dock = DockStyle.Top,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(31, 41, 55),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += (_, _) => NavigateTo(button, module);
            _navButtons.Add(button);
        }

        for (var i = _navButtons.Count - 1; i >= 0; i--)
        {
            sidebarPanel.Controls.Add(_navButtons[i]);
        }
    }

    private void NavigateTo(Button button, NavigationItem module)
    {
        if (_activeButton is not null)
        {
            _activeButton.BackColor = Color.FromArgb(31, 41, 55);
        }

        _activeButton = button;
        button.BackColor = Color.FromArgb(37, 99, 235);
        headerTitleLabel.Text = module.Title;
        _navigator.Show(contentPanel, module.PageType, module.Title);
        statusLabel.Text = $"Ready  •  {module.Title}";
    }
}
