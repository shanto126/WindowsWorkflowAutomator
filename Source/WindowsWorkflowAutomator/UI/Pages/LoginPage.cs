using WindowsWorkflowAutomator.Authentication;
using WindowsWorkflowAutomator.UI;
using WindowsWorkflowAutomator.UI.Theme;

namespace WindowsWorkflowAutomator.UI.Pages;

public sealed class LoginPage : UserControl
{
    private readonly AuthApiClient _auth;
    private readonly AppSession _session;
    private readonly TextBox _emailBox = new();
    private readonly TextBox _passwordBox = new();
    private readonly Button _loginButton = new();
    private readonly Label _statusLabel = new();

    public LoginPage(AuthApiClient auth, AppSession session)
    {
        _auth = auth;
        _session = session;
        Dock = DockStyle.Fill;
        BackColor = AppTheme.Background;
        BuildLayout();
    }

    private void BuildLayout()
    {
        var title = new Label
        {
            Text = "Welcome to Windows Workflow Automator",
            AutoSize = true,
            Font = AppTheme.Title,
            ForeColor = AppTheme.TextPrimary
        };
        var subtitle = new Label
        {
            Text = "Sign in to manage your subscription and automation workspace.",
            AutoSize = true,
            Font = AppTheme.Body,
            ForeColor = AppTheme.TextSecondary,
            Padding = new Padding(0, 8, 0, 20)
        };
        var emailLabel = new Label { Text = "Email", AutoSize = true };
        _emailBox.Text = "demo@windowsworkflowautomator.local";
        _emailBox.Width = 360;
        _emailBox.Margin = new Padding(0, 4, 0, 12);
        var passwordLabel = new Label { Text = "Password", AutoSize = true };
        _passwordBox.UseSystemPasswordChar = true;
        _passwordBox.Text = "demo123";
        _passwordBox.Width = 360;
        _passwordBox.Margin = new Padding(0, 4, 0, 16);
        _loginButton.Text = "Login";
        _loginButton.AutoSize = true;
        _loginButton.Click += OnLogin;
        _loginButton.ApplyPrimaryStyle();
        _statusLabel.AutoSize = true;
        _statusLabel.ForeColor = Color.Firebrick;
        _statusLabel.Padding = new Padding(0, 12, 0, 0);

        var panel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(32),
            BackColor = AppTheme.CardBackground
        };
        panel.Controls.Add(title);
        panel.Controls.Add(subtitle);
        panel.Controls.Add(emailLabel);
        panel.Controls.Add(_emailBox);
        panel.Controls.Add(passwordLabel);
        panel.Controls.Add(_passwordBox);
        panel.Controls.Add(_loginButton);
        panel.Controls.Add(_statusLabel);

        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1
        };
        host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        host.Controls.Add(panel, 0, 0);
        panel.Anchor = AnchorStyles.None;
        Controls.Add(host);
    }

    private async void OnLogin(object? sender, EventArgs e)
    {
        try
        {
            _loginButton.Enabled = false;
            _statusLabel.Text = "Signing in...";
            var result = await _auth.LoginAsync(
                _emailBox.Text,
                _passwordBox.Text);
            if (result is null)
            {
                _statusLabel.Text = "LicenseServer is unavailable.";
                return;
            }

            if (!result.Success)
            {
                _statusLabel.Text = "Invalid email or password.";
                return;
            }

            _session.SignIn(result.UserId, result.Role);
        }
        catch (HttpRequestException)
        {
            _statusLabel.Text = "LicenseServer is unavailable.";
        }
        catch (TaskCanceledException)
        {
            _statusLabel.Text = "Login request timed out.";
        }
        finally
        {
            _loginButton.Enabled = true;
        }
    }
}
