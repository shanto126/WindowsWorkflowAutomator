using WindowsWorkflowAutomator.Authentication;
using WindowsWorkflowAutomator.Subscriptions;
using WindowsWorkflowAutomator.UI;
using WindowsWorkflowAutomator.UI.Navigation;
using WindowsWorkflowAutomator.UI.Theme;

namespace WindowsWorkflowAutomator.UI.Pages;

public sealed class UserDashboardPage : UserControl
{
    private readonly AppSession _session;
    private readonly SubscriptionApiClient _subscriptions;
    private readonly ModuleNavigator _navigator;
    private readonly Label _welcomeLabel = new();
    private readonly Label _planLabel = new();
    private readonly Label _licenseLabel = new();
    private readonly Label _expiryLabel = new();
    private readonly DataGridView _paymentsGrid = new();

    public UserDashboardPage(
        AppSession session,
        SubscriptionApiClient subscriptions,
        ModuleNavigator navigator)
    {
        _session = session;
        _subscriptions = subscriptions;
        _navigator = navigator;
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
            Text = "User Dashboard",
            AutoSize = true,
            Font = AppTheme.Title,
            ForeColor = AppTheme.TextPrimary,
            Dock = DockStyle.Top
        };
        _welcomeLabel.AutoSize = true;
        _welcomeLabel.Font = AppTheme.Heading;
        _welcomeLabel.Padding = new Padding(0, 6, 0, 16);
        _planLabel.AutoSize = true;
        _licenseLabel.AutoSize = true;
        _expiryLabel.AutoSize = true;
        foreach (var label in new[] { _planLabel, _licenseLabel, _expiryLabel })
        {
            label.Font = AppTheme.Body;
            label.Padding = new Padding(0, 3, 0, 3);
        }

        var buyButton = new Button { Text = "Buy Subscription", AutoSize = true };
        buyButton.ApplyPrimaryStyle();
        buyButton.Click += (_, _) => ShowSubscription();
        var logoutButton = new Button { Text = "Logout", AutoSize = true };
        logoutButton.ApplySecondaryStyle();
        logoutButton.Click += (_, _) => _session.SignOut();
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, WrapContents = false };
        actions.Controls.Add(buyButton);
        actions.Controls.Add(logoutButton);

        _paymentsGrid.Dock = DockStyle.Fill;
        _paymentsGrid.ReadOnly = true;
        _paymentsGrid.AllowUserToAddRows = false;
        _paymentsGrid.AutoGenerateColumns = true;
        _paymentsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _paymentsGrid.RowHeadersVisible = false;
        _paymentsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _paymentsGrid.ApplyModernStyle();

        var details = new Panel { Dock = DockStyle.Top, AutoSize = true, BackColor = AppTheme.CardBackground, Padding = new Padding(16) };
        details.Controls.Add(_expiryLabel);
        details.Controls.Add(_licenseLabel);
        details.Controls.Add(_planLabel);
        details.Controls.Add(_welcomeLabel);

        var historyTitle = new Label
        {
            Text = "Payment history",
            AutoSize = true,
            Font = AppTheme.Heading,
            Dock = DockStyle.Top,
            Padding = new Padding(0, 14, 0, 8)
        };
        var body = new Panel { Dock = DockStyle.Fill };
        body.Controls.Add(_paymentsGrid);
        body.Controls.Add(historyTitle);
        body.Controls.Add(actions);
        body.Controls.Add(details);

        Controls.Add(body);
        Controls.Add(title);
    }

    private async void OnLoad(object? sender, EventArgs e) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        if (!_session.IsAuthenticated)
        {
            return;
        }

        try
        {
            var dashboard = await _subscriptions.GetUserDashboardAsync(_session.UserId);
            if (dashboard is null)
            {
                return;
            }

            _welcomeLabel.Text = $"Welcome {dashboard.UserName}";
            _planLabel.Text = $"Current subscription: {dashboard.PlanName}";
            _licenseLabel.Text = $"License status: {dashboard.LicenseStatus}";
            _expiryLabel.Text = $"Expiry date: {(dashboard.ExpiryDate.HasValue ? dashboard.ExpiryDate.Value.ToLocalTime().ToString("d") : "N/A")}";
            _paymentsGrid.DataSource = dashboard.PaymentHistory;
        }
        catch (HttpRequestException)
        {
            _licenseLabel.Text = "License status: LicenseServer unavailable";
        }
    }

    private void ShowSubscription()
    {
        var form = FindForm();
        var contentPanel = form?.Controls.Find("contentPanel", true).FirstOrDefault() as Panel;
        if (contentPanel is not null)
        {
            _navigator.Show(contentPanel, typeof(SubscriptionPage), "Subscription");
        }
    }
}
