using WindowsWorkflowAutomator.Subscriptions;
using WindowsWorkflowAutomator.UI;
using WindowsWorkflowAutomator.UI.Theme;

namespace WindowsWorkflowAutomator.UI.Pages;

public sealed class AdminPage : UserControl
{
    private readonly SubscriptionApiClient _subscriptions;
    private readonly DataGridView _usersGrid = new();
    private readonly DataGridView _paymentsGrid = new();
    private readonly DataGridView _licensesGrid = new();
    private readonly Label _statusLabel = new();

    public AdminPage(SubscriptionApiClient subscriptions)
    {
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
            Text = "Admin",
            AutoSize = true,
            Font = AppTheme.Title,
            ForeColor = AppTheme.TextPrimary,
            Dock = DockStyle.Top
        };
        var refresh = new Button
        {
            Text = "Refresh",
            AutoSize = true,
            Height = 32,
            Dock = DockStyle.Left
        };
        refresh.ApplyPrimaryStyle();
        refresh.Click += async (_, _) => await RefreshAsync();
        _statusLabel.AutoSize = true;
        _statusLabel.Font = AppTheme.Body;
        _statusLabel.ForeColor = AppTheme.TextSecondary;
        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.Padding = new Padding(12, 8, 0, 0);

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 42 };
        toolbar.Controls.Add(_statusLabel);
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
        Controls.Add(title);
    }

    private async void OnLoad(object? sender, EventArgs e) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        try
        {
            var users = await _subscriptions.GetUsersAsync();
            var payments = await _subscriptions.GetPaymentsAsync();
            var licenses = await _subscriptions.GetLicensesAsync();
            _usersGrid.DataSource = users;
            _paymentsGrid.DataSource = payments;
            _licensesGrid.DataSource = licenses;
            _statusLabel.Text = $"Users: {users.Count}  Payments: {payments.Count}  Licenses: {licenses.Count}";
        }
        catch (HttpRequestException)
        {
            _statusLabel.Text = "The LicenseServer is unavailable.";
        }
        catch (TaskCanceledException)
        {
            _statusLabel.Text = "The admin request timed out.";
        }
    }

    private static void ConfigureGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
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
