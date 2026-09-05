using WindowsWorkflowAutomator.Authentication;
using WindowsWorkflowAutomator.Licensing;
using WindowsWorkflowAutomator.Subscriptions;
using WindowsWorkflowAutomator.UI;
using WindowsWorkflowAutomator.UI.Theme;

namespace WindowsWorkflowAutomator.UI.Pages;

public sealed class SubscriptionPage : UserControl
{
    private const int PremiumPlanId = 3;
    private const decimal PremiumPrice = 999;

    private readonly SubscriptionApiClient _subscriptions;
    private readonly ILicenseService _licenseService;
    private readonly AppSession _session;
    private readonly Label _currentPlan = new();
    private readonly Label _licenseStatus = new();
    private readonly Label _expiryDate = new();
    private readonly Label _resultLabel = new();
    private readonly Button _buyButton = new();

    public SubscriptionPage(
        SubscriptionApiClient subscriptions,
        ILicenseService licenseService,
        AppSession session)
    {
        _subscriptions = subscriptions;
        _licenseService = licenseService;
        _session = session;
        Dock = DockStyle.Fill;
        BackColor = AppTheme.Background;
        Padding = new Padding(30);
        BuildLayout();
        Load += OnLoad;
    }

    private void BuildLayout()
    {
        var title = new Label
        {
            Text = "Subscription",
            AutoSize = true,
            Font = AppTheme.Title,
            ForeColor = AppTheme.TextPrimary,
            Margin = new Padding(0, 0, 0, 4)
        };
        var subtitle = new Label
        {
            Text = "Upgrade this demo account through the simulated payment flow.",
            AutoSize = true,
            Font = AppTheme.Body,
            ForeColor = AppTheme.TextSecondary,
            Margin = new Padding(0, 0, 0, 18)
        };

        var currentCard = CreateCard("Current Plan");
        ConfigureValue(_currentPlan, "Free", AppTheme.Heading, AppTheme.TextPrimary);
        ConfigureValue(_licenseStatus, "Expired", AppTheme.Body, AppTheme.TextPrimary);
        ConfigureValue(_expiryDate, "N/A", AppTheme.Body, AppTheme.TextPrimary);
        AddDetailRow(currentCard, "Plan", _currentPlan, 0);
        AddDetailRow(currentCard, "Status", _licenseStatus, 1);
        AddDetailRow(currentCard, "Expiry Date", _expiryDate, 2);

        var availableCard = CreateCard("Available Plans");
        var planContent = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0)
        };
        planContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        planContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        planContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        planContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var planTitle = new Label
        {
            Text = "Premium Plan",
            AutoSize = true,
            Font = AppTheme.Heading,
            ForeColor = AppTheme.TextPrimary,
            Margin = new Padding(0, 0, 0, 6)
        };
        var planDetails = new Label
        {
            Text = "৳999 / 365 Days",
            AutoSize = true,
            Font = AppTheme.Body,
            ForeColor = AppTheme.TextSecondary,
            Margin = new Padding(0, 0, 0, 14)
        };
        _buyButton.Text = "Buy Premium";
        _buyButton.AutoSize = true;
        _buyButton.Anchor = AnchorStyles.Left;
        _buyButton.ApplyPrimaryStyle();
        _buyButton.Click += OnBuyPremium;

        planContent.Controls.Add(planTitle, 0, 0);
        planContent.Controls.Add(planDetails, 0, 1);
        planContent.Controls.Add(_buyButton, 0, 2);
        availableCard.Controls.Add(planContent, 0, 1);

        _resultLabel.AutoSize = true;
        _resultLabel.Font = AppTheme.Body;
        _resultLabel.ForeColor = AppTheme.TextSecondary;
        _resultLabel.Margin = new Padding(0, 16, 0, 0);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var row = 0; row < 5; row++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(subtitle, 0, 1);
        layout.Controls.Add(currentCard, 0, 2);
        layout.Controls.Add(availableCard, 0, 3);
        layout.Controls.Add(_resultLabel, 0, 4);
        Controls.Add(layout);
    }

    private async void OnLoad(object? sender, EventArgs e)
    {
        await RefreshCurrentPlanAsync();
    }

    private async void OnBuyPremium(object? sender, EventArgs e)
    {
        using var dialog = new DemoPaymentDialog();
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        try
        {
            _buyButton.Enabled = false;
            _resultLabel.Text = "Processing demo payment...";
            var payment = await _subscriptions.ProcessPaymentAsync(
                _session.UserId,
                PremiumPlanId,
                dialog.PaymentMethod,
                PremiumPrice);

            if (payment is null || !payment.Success)
            {
                _resultLabel.Text = payment?.Message ?? "The LicenseServer is unavailable.";
                return;
            }

            var activated = await _licenseService.ActivateAsync(payment.LicenseKey);
            if (!activated)
            {
                _resultLabel.Text = "Payment succeeded, but the desktop license could not be activated locally.";
                return;
            }

            _resultLabel.Text =
                $"Payment Successful\r\n\r\nYour License:\r\n{payment.LicenseKey}\r\n\r\nPlan:\r\nPremium\r\n\r\nExpiry:\r\n{DateTimeOffset.UtcNow.AddDays(365):d}";
            await RefreshCurrentPlanAsync();
        }
        catch (HttpRequestException)
        {
            _resultLabel.Text = "The LicenseServer is unavailable. Start it and try again.";
        }
        catch (TaskCanceledException)
        {
            _resultLabel.Text = "The demo payment request timed out.";
        }
        finally
        {
            _buyButton.Enabled = true;
        }
    }

    private async Task RefreshCurrentPlanAsync()
    {
        var valid = await _licenseService.ValidateAsync();
        _currentPlan.Text = valid ? _licenseService.CurrentTier : "Free";
        _licenseStatus.Text = valid ? "Active" : "Expired";
        _expiryDate.Text = valid && _licenseService.IsPremium
            ? "Active license"
            : "N/A";
    }

    private static TableLayoutPanel CreateCard(string title)
    {
        var card = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(16),
            Margin = new Padding(0, 0, 0, 16),
            ColumnCount = 1,
            RowCount = 2,
            BackColor = AppTheme.CardBackground,
            BorderStyle = BorderStyle.FixedSingle
        };
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var heading = new Label
        {
            Text = title,
            AutoSize = true,
            Font = AppTheme.Heading,
            ForeColor = AppTheme.TextPrimary,
            Margin = new Padding(0, 0, 0, 12)
        };
        card.Controls.Add(heading, 0, 0);
        return card;
    }

    private static void AddDetailRow(
        TableLayoutPanel card,
        string labelText,
        Control value,
        int row)
    {
        var details = card.GetControlFromPosition(0, 1) as TableLayoutPanel;
        if (details is null)
        {
            details = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 0,
                Margin = new Padding(0)
            };
            details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
            card.Controls.Add(details, 0, 1);
        }

        details.RowCount = row + 1;
        details.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var label = new Label
        {
            Text = labelText,
            AutoSize = true,
            Font = AppTheme.Body,
            ForeColor = AppTheme.TextSecondary,
            Margin = new Padding(0, 0, 12, 8)
        };
        value.Margin = new Padding(0, 0, 0, 8);
        details.Controls.Add(label, 0, row);
        details.Controls.Add(value, 1, row);
    }

    private static void ConfigureValue(
        Label label,
        string text,
        Font font,
        Color color)
    {
        label.Text = text;
        label.AutoSize = true;
        label.Font = font;
        label.ForeColor = color;
    }
}
