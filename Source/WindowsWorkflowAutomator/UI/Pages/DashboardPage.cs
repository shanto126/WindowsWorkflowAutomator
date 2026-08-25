using System.Linq;
using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Services.Automation;
using WindowsWorkflowAutomator.Services.Scheduler;
using WindowsWorkflowAutomator.Repositories;
using WindowsWorkflowAutomator.Licensing;
using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.UI.Pages;

public sealed class DashboardPage : UserControl
{
    private readonly IWorkflowService _workflows;
    private readonly ISchedulerService _scheduler;
    private readonly ISocialPostRepository _socialPosts;
    private readonly ILicenseService _licenseService;
    private readonly IAppLogger _logger;
    private readonly WindowsWorkflowAutomator.UI.Navigation.ModuleNavigator _navigator;

    private readonly Label _workflowsCount = new();
    private readonly Label _schedulesCount = new();
    private readonly Label _queuedPostsCount = new();
    private readonly Label _licenseStatus = new();

    public DashboardPage(
        IWorkflowService workflows,
        ISchedulerService scheduler,
        ISocialPostRepository socialPosts,
        ILicenseService licenseService,
        IAppLogger logger,
        WindowsWorkflowAutomator.UI.Navigation.ModuleNavigator navigator)
    {
        _workflows = workflows;
        _scheduler = scheduler;
        _socialPosts = socialPosts;
        _licenseService = licenseService;
        _logger = logger;
        _navigator = navigator;

        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(246, 248, 252);
        Padding = new Padding(24);

        BuildLayout();

        Load += OnLoad;
    }

    private void BuildLayout()
    {
        var title = new Label
        {
            Text = "Dashboard",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
            ForeColor = Color.FromArgb(17, 24, 39),
            Dock = DockStyle.Top
        };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 4,
            RowCount = 1,
            AutoSize = true,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            Padding = new Padding(0, 12, 0, 12)
        };

        for (var i = 0; i < 4; i++)
        {
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        }

        grid.Controls.Add(CreateCard("Workflows", _workflowsCount, "Total saved workflows.", typeof(WorkflowAutomationPage)), 0, 0);
                grid.Controls.Add(CreateCard("Scheduled Tasks", _schedulesCount, "Tasks defined in the scheduler.", typeof(TaskSchedulerPage)), 1, 0);
                grid.Controls.Add(CreateCard("Queued Social Posts", _queuedPostsCount, "Posts pending publication.", typeof(SocialMediaManagerPage)), 2, 0);
                grid.Controls.Add(CreateCard("License", _licenseStatus, "Current license tier and expiry.", typeof(SettingsPage)), 3, 0);

        var refreshButton = new Button
        {
            Text = "Refresh",
            Height = 34,
            AutoSize = true,
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 12, 0, 0)
        };
        refreshButton.FlatAppearance.BorderSize = 0;
        refreshButton.Click += async (_, _) => await RefreshCountsAsync();

        Controls.Add(grid);
        Controls.Add(refreshButton);
        Controls.Add(title);
    }

    private Panel CreateCard(string title, Label valueLabel, string subtitle, Type? targetPage = null)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            Margin = new Padding(8),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            AutoSize = true,
            Cursor = Cursors.Hand
        };

        var titleLabel = new Label
        {
            Text = title,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 41, 55)
        };

        valueLabel.Text = "—";
        valueLabel.AutoSize = true;
        valueLabel.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
        valueLabel.ForeColor = Color.FromArgb(17, 24, 39);
        valueLabel.Padding = new Padding(0, 6, 0, 6);

        var subtitleLabel = new Label
        {
            Text = subtitle,
            AutoSize = true,
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(75, 85, 99)
        };

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true
        };

        layout.Controls.Add(titleLabel);
        layout.Controls.Add(valueLabel);
        layout.Controls.Add(subtitleLabel);

        panel.Controls.Add(layout);

        if (targetPage is not null)
        {
            panel.Click += (_, _) => TryNavigateTo(targetPage, title);
            // Also forward clicks from contained controls
            foreach (Control c in layout.Controls)
            {
                c.Click += (_, _) => TryNavigateTo(targetPage, title);
            }
        }

        return panel;
    }

    private void TryNavigateTo(Type pageType, string title)
    {
        try
        {
            var form = FindForm();
            if (form is null)
            {
                return;
            }

            // find contentPanel by name in the form's controls
            Panel? contentPanel = null;
            foreach (Control c in form.Controls)
            {
                if (c is Panel p && p.Name == "contentPanel")
                {
                    contentPanel = p;
                    break;
                }

                // search container controls
                foreach (Control child in c.Controls)
                {
                    if (child is Panel cp && cp.Name == "contentPanel")
                    {
                        contentPanel = cp;
                        break;
                    }
                }

                if (contentPanel is not null) break;
            }

            if (contentPanel is not null)
            {
                _navigator.Show(contentPanel, pageType, title);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to navigate from dashboard to {title}", ex);
        }
    }

    private async void OnLoad(object? sender, EventArgs e)
    {
        await RefreshCountsAsync();
    }

    private async Task RefreshCountsAsync()
    {
        try
        {
            var workflows = await _workflows.GetAllAsync();
            var schedules = await _scheduler.GetAllAsync();
            var queued = await _socialPosts.GetByStatusAsync(PostQueueStatus.Pending);

            _workflowsCount.Text = workflows?.Count.ToString() ?? "0";
            _schedulesCount.Text = schedules?.Count.ToString() ?? "0";
            _queuedPostsCount.Text = queued?.Count.ToString() ?? "0";

            await _licenseService.ValidateAsync();
            _licenseStatus.Text = _licenseService.IsPremium ? $"Premium ({_licenseService.CurrentTier})" : $"Free ({_licenseService.CurrentTier})";
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to refresh dashboard counts.", ex);
            _workflowsCount.Text = "—";
            _schedulesCount.Text = "—";
            _queuedPostsCount.Text = "—";
            _licenseStatus.Text = "Unknown";
        }
    }
}
