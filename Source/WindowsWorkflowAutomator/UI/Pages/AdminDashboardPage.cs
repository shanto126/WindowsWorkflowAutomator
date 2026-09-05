using WindowsWorkflowAutomator.Authentication;
using WindowsWorkflowAutomator.Subscriptions;
using WindowsWorkflowAutomator.UI;
using WindowsWorkflowAutomator.UI.Theme;

namespace WindowsWorkflowAutomator.UI.Pages;

public sealed class AdminDashboardPage : UserControl
{
    private readonly AppSession _session;
    private readonly SubscriptionApiClient _subscriptions;
    private readonly Label _usersCount = new();
    private readonly Label _subscriptionsCount = new();
    private readonly Label _revenue = new();
    private readonly Label _licensesCount = new();
    private readonly DataGridView _usersGrid = new();
    private readonly DataGridView _paymentsGrid = new();
    private readonly DataGridView _licensesGrid = new();
    private readonly Label _statusLabel = new();

    public AdminDashboardPage(AppSession session, SubscriptionApiClient subscriptions)
    {
        _session = session;
        _subscriptions = subscriptions;
        Dock = DockStyle.Fill;
        BackColor = AppTheme.Background;
        Padding = new Padding(24);
        BuildLayout();
        Load += OnLoad;
    }

    private void BuildLayout()
    {
        var title = new Label
        {
            Text = "Admin Dashboard",
            AutoSize = true,
            Font = AppTheme.Title,
            ForeColor = AppTheme.TextPrimary,
            Dock = DockStyle.Top
        };
        var cards = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 92,
            ColumnCount = 4,
            Padding = new Padding(0, 12, 0, 12)
        };
        for (var i = 0; i < 4; i++)
        {
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        }
        cards.Controls.Add(CreateCard("Total Users", _usersCount), 0, 0);
        cards.Controls.Add(CreateCard("Active Subscriptions", _subscriptionsCount), 1, 0);
        cards.Controls.Add(CreateCard("Total Revenue", _revenue), 2, 0);
        cards.Controls.Add(CreateCard("Active Licenses", _licensesCount), 3, 0);

        var refresh = new Button { Text = "Refresh", AutoSize = true };
        refresh.ApplyPrimaryStyle();
        refresh.Click += async (_, _) => await RefreshAsync();
        var logout = new Button { Text = "Logout", AutoSize = true };
        logout.ApplySecondaryStyle();
        logout.Click += (_, _) => _session.SignOut();
        _statusLabel.AutoSize = true;
        _statusLabel.Padding = new Padding(12, 8, 0, 0);
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 42 };
        toolbar.Controls.Add(_statusLabel);
        toolbar.Controls.Add(logout);
        toolbar.Controls.Add(refresh);

        ConfigureGrid(_usersGrid);
        ConfigureGrid(_paymentsGrid);
        ConfigureGrid(_licensesGrid);
        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(CreateTab("Users", _usersGrid));
        tabs.TabPages.Add(CreateTab("Payments", _paymentsGrid));
        tabs.TabPages.Add(CreateTab("Licenses", _licensesGrid));

        Controls.Add(tabs);
        Controls.Add(toolbar);
        Controls.Add(cards);
        Controls.Add(title);
    }

    private async void OnLoad(object? sender, EventArgs e) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        try
        {
            var dashboard = await _subscriptions.GetAdminDashboardAsync();
            var users = await _subscriptions.GetUsersAsync();
            var payments = await _subscriptions.GetPaymentsAsync();
            var licenses = await _subscriptions.GetLicensesAsync();
            if (dashboard is not null)
            {
                _usersCount.Text = dashboard.TotalUsers.ToString();
                _subscriptionsCount.Text = dashboard.ActiveSubscriptions.ToString();
                _revenue.Text = $"৳{dashboard.TotalRevenue:N0}";
                _licensesCount.Text = dashboard.ActiveLicenses.ToString();
            }
            _usersGrid.DataSource = users;
            _paymentsGrid.DataSource = payments;
            _licensesGrid.DataSource = licenses;
            _statusLabel.Text = "Dashboard refreshed";
        }
        catch (HttpRequestException)
        {
            _statusLabel.Text = "LicenseServer unavailable";
        }
        catch (TaskCanceledException)
        {
            _statusLabel.Text = "Dashboard request timed out";
        }
    }

    private static Panel CreateCard(string title, Label value)
    {
        value.AutoSize = true;
        value.Font = AppTheme.Heading;
        value.ForeColor = AppTheme.TextPrimary;
        var titleLabel = new Label { Text = title, AutoSize = true, ForeColor = AppTheme.TextSecondary, Dock = DockStyle.Top };
        var panel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(4), Padding = new Padding(10), BackColor = AppTheme.CardBackground, BorderStyle = BorderStyle.FixedSingle };
        panel.Controls.Add(value);
        panel.Controls.Add(titleLabel);
        return panel;
    }

    private static void ConfigureGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AutoGenerateColumns = true;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.ApplyModernStyle();
    }

    private static TabPage CreateTab(string title, Control content)
    {
        var tab = new TabPage(title);
        tab.Controls.Add(content);
        return tab;
    }
}
