using WindowsWorkflowAutomator.Licensing;
using WindowsWorkflowAutomator.Logging;

namespace WindowsWorkflowAutomator.UI.Pages;

public sealed class SettingsPage : UserControl
{
    private readonly ILicenseService _licenseService;
    private readonly IAppLogger _logger;

    private readonly TextBox _licenseKeyBox = new();
    private readonly Label _tierLabel = new();
    private readonly Label _statusLabel = new();

    private Button _activateButton = null!;
    private Button _validateButton = null!;
    private Button _deactivateButton = null!;

    public SettingsPage(
        ILicenseService licenseService,
        IAppLogger logger)
    {
        _licenseService = licenseService;
        _logger = logger;

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
            Text = "Settings",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
            ForeColor = Color.FromArgb(17, 24, 39),
            Dock = DockStyle.Top
        };

        var subtitle = new Label
        {
            Text = "Manage application settings and license activation.",
            AutoSize = true,
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.FromArgb(75, 85, 99),
            Padding = new Padding(0, 4, 0, 16),
            Dock = DockStyle.Top
        };

        var licenseTitle = new Label
        {
            Text = "License",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 12F),
            ForeColor = Color.FromArgb(17, 24, 39),
            Dock = DockStyle.Top,
            Padding = new Padding(0, 8, 0, 8)
        };

        _licenseKeyBox.Dock = DockStyle.Fill;
        _licenseKeyBox.Margin = new Padding(0, 4, 8, 4);
        _licenseKeyBox.PlaceholderText = "Enter your license key";
        _licenseKeyBox.MaxLength = 64;

        _activateButton = CreateButton("Activate", OnActivate);
        _validateButton = CreateButton("Validate", OnValidate);
        _deactivateButton = CreateButton("Deactivate", OnDeactivate);

        var licenseFields = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 3,
            Padding = new Padding(0, 0, 0, 12)
        };

        licenseFields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 150));

        licenseFields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        licenseFields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 120));

        AddField(
            licenseFields,
            0,
            "License Key",
            _licenseKeyBox,
            _activateButton);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 42,
            WrapContents = false
        };

        buttons.Controls.Add(_validateButton);
        buttons.Controls.Add(_deactivateButton);

        _tierLabel.Text = "Current tier: Free";
        _tierLabel.AutoSize = true;
        _tierLabel.Font = new Font("Segoe UI Semibold", 10.5F);
        _tierLabel.ForeColor = Color.FromArgb(17, 24, 39);
        _tierLabel.Dock = DockStyle.Top;
        _tierLabel.Padding = new Padding(0, 8, 0, 4);

        _statusLabel.Text = "License status: Not validated.";
        _statusLabel.AutoSize = true;
        _statusLabel.Font = new Font("Segoe UI", 10F);
        _statusLabel.ForeColor = Color.FromArgb(180, 83, 9);
        _statusLabel.Dock = DockStyle.Top;
        _statusLabel.Padding = new Padding(0, 4, 0, 8);

        var licensePanel = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true
        };

        licensePanel.Controls.Add(_statusLabel);
        licensePanel.Controls.Add(_tierLabel);
        licensePanel.Controls.Add(buttons);
        licensePanel.Controls.Add(licenseFields);
        licensePanel.Controls.Add(licenseTitle);

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };

        body.Controls.Add(licensePanel);

        Controls.Add(body);
        Controls.Add(subtitle);
        Controls.Add(title);
    }

    private async void OnLoad(object? sender, EventArgs e)
    {
        await RefreshLicenseStatusAsync();
    }

    private async void OnActivate(object? sender, EventArgs e)
    {
        var licenseKey = _licenseKeyBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(licenseKey))
        {
            ShowWarning("Enter a license key first.");
            return;
        }

        try
        {
            SetButtonsEnabled(false);

            var success = await _licenseService.ActivateAsync(licenseKey);

            if (success)
            {
                _licenseKeyBox.Clear();

                await RefreshLicenseStatusAsync();

                MessageBox.Show(
                    FindForm(),
                    "License activated successfully.",
                    "License",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                await RefreshLicenseStatusAsync();

                ShowWarning("License activation failed.");
            }
        }
        catch (Exception ex)
        {
            _logger.Error("License activation failed.", ex);
            ShowWarning(ex.Message);
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }

    private async void OnValidate(object? sender, EventArgs e)
    {
        try
        {
            SetButtonsEnabled(false);

            var valid = await _licenseService.ValidateAsync();

            await RefreshLicenseStatusAsync();

            if (!valid)
            {
                ShowWarning("License validation failed.");
            }
        }
        catch (Exception ex)
        {
            _logger.Error("License validation failed.", ex);
            ShowWarning(ex.Message);
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }

    private async void OnDeactivate(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            FindForm(),
            "Are you sure you want to deactivate the current license?",
            "Deactivate License",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            SetButtonsEnabled(false);

            await _licenseService.DeactivateAsync();

            await RefreshLicenseStatusAsync();

            MessageBox.Show(
                FindForm(),
                "License deactivated.",
                "License",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _logger.Error("License deactivation failed.", ex);
            ShowWarning(ex.Message);
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }

    private async Task RefreshLicenseStatusAsync()
    {
        try
        {
            await _licenseService.ValidateAsync();

            _tierLabel.Text =
                $"Current tier: {_licenseService.CurrentTier}";

            if (_licenseService.IsPremium)
            {
                _statusLabel.Text = "License status: Premium / Active.";
                _statusLabel.ForeColor = Color.FromArgb(21, 128, 61);
            }
            else
            {
                _statusLabel.Text = "License status: Free / Inactive.";
                _statusLabel.ForeColor = Color.FromArgb(180, 83, 9);
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Could not refresh license status.", ex);

            _tierLabel.Text = "Current tier: Unknown";
            _statusLabel.Text = "License status: Unable to validate.";
            _statusLabel.ForeColor = Color.FromArgb(180, 83, 9);
        }
    }

    private void SetButtonsEnabled(bool enabled)
    {
        _activateButton.Enabled = enabled;
        _validateButton.Enabled = enabled;
        _deactivateButton.Enabled = enabled;
    }

    private void ShowWarning(string message)
    {
        MessageBox.Show(
            FindForm(),
            message,
            "License",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    private static Button CreateButton(
        string text,
        EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 32,
            Margin = new Padding(0, 0, 8, 0),
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        button.FlatAppearance.BorderSize = 0;
        button.Click += onClick;

        return button;
    }

    private static void AddField(
        TableLayoutPanel host,
        int row,
        string label,
        Control editor,
        Control? sideButton = null)
    {
        host.RowStyles.Add(
            new RowStyle(SizeType.AutoSize));

        var caption = new Label
        {
            Text = label,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 8, 12, 4)
        };

        editor.Dock = DockStyle.Fill;
        editor.Margin = new Padding(0, 4, 8, 4);

        host.Controls.Add(caption, 0, row);
        host.Controls.Add(editor, 1, row);

        if (sideButton is not null)
        {
            sideButton.Margin = new Padding(0, 4, 0, 4);
            host.Controls.Add(sideButton, 2, row);
        }
        else
        {
            host.Controls.Add(
                new Panel { Width = 1 },
                2,
                row);
        }
    }
}