using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.UI.Navigation;
using WindowsWorkflowAutomator.UI.Pages;

namespace WindowsWorkflowAutomator.UI;

public partial class MainForm : Form
{
    private readonly ModuleNavigator _navigator;
    private readonly IAppLogger _logger;
    private readonly List<Button> _navButtons = [];
    private Button? _activeButton;

    public MainForm(ModuleNavigator navigator, IAppLogger logger)
    {
        _navigator = navigator;
        _logger = logger;
        InitializeComponent();
        BuildNavigation();
        NavigateTo(_navButtons[0], CreateModules()[0]);
        _logger.Information("Main window ready.");
    }

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
        new() { Title = "Settings", PageType = typeof(SettingsPage) }
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
