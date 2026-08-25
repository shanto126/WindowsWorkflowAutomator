using WindowsWorkflowAutomator.GitHub;
using WindowsWorkflowAutomator.Licensing;

namespace WindowsWorkflowAutomator.UI.Pages;

public sealed class GitHubAutomationPage : UserControl
{
    private readonly IGitHubService _gitHubService;
    private readonly ILicenseService _licenseService;

    private readonly TextBox _localPathBox = new();
    private readonly TextBox _remoteUrlBox = new();
    private readonly TextBox _branchBox = new();
    private readonly TextBox _templateBox = new();
    private readonly TextBox _patBox = new();
    private readonly TextBox _commitMessageBox = new();
    private readonly Label _statusLabel = new();
    private readonly ListBox _changedFilesList = new();
    private readonly ListBox _activityList = new();

    // New controls for sync mode
    private readonly ComboBox _syncModeCombo = new();
    private readonly ComboBox _inactivityCombo = new();
    private readonly Label _autoSyncStatusLabel = new();

    private bool _tokenEdited;

    public GitHubAutomationPage(IGitHubService gitHubService, ILicenseService licenseService)
    {
        _gitHubService = gitHubService;
        _licenseService = licenseService;

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
            Text = "GitHub Automation",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
            ForeColor = Color.FromArgb(17, 24, 39),
            Dock = DockStyle.Top
        };
        var subtitle = new Label
        {
            Text = "Configure a local repository, review status, commit locally, and push only when explicitly requested.",
            AutoSize = true,
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.FromArgb(75, 85, 99),
            Padding = new Padding(0, 4, 0, 12),
            MaximumSize = new Size(950, 0),
            Dock = DockStyle.Top
        };

        _patBox.UseSystemPasswordChar = true;
        _patBox.TextChanged += (_, _) => _tokenEdited = true;
        _templateBox.Text = "chore: backup changes";
        _branchBox.Text = "develop";

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            GrowStyle = TableLayoutPanelGrowStyle.AddRows,
            ColumnCount = 3,
            Padding = new Padding(0, 0, 0, 8)
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));

        AddFieldRow(form, 0, "Local repository folder", _localPathBox, CreateButton("Browse…", OnBrowseFolder));
        AddFieldRow(form, 1, "Remote URL (origin)", _remoteUrlBox);
        AddFieldRow(form, 2, "Branch", _branchBox);
        AddFieldRow(form, 3, "Commit template", _templateBox);
        AddFieldRow(form, 4, "Personal Access Token", _patBox);

        // Sync mode and inactivity settings
        _syncModeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _syncModeCombo.Items.AddRange(new object[] { "Manual", "Smart Auto Sync", "Scheduled (Coming Soon)" });
        _syncModeCombo.SelectedIndex = 0;

        _inactivityCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _inactivityCombo.Items.AddRange(new object[] { "30 seconds", "1 minute", "5 minutes" });
        _inactivityCombo.SelectedIndex = 0;

        AddFieldRow(form, 5, "Sync mode", _syncModeCombo, _inactivityCombo);

        var tokenNote = new Label
        {
            Text = "Token is stored encrypted in local app settings. Leave empty to keep existing token.",
            AutoSize = true,
            Dock = DockStyle.Top,
            ForeColor = Color.FromArgb(75, 85, 99),
            Padding = new Padding(0, 0, 0, 8)
        };

        _commitMessageBox.PlaceholderText = "Optional commit message (uses template if empty)";
        var commitMessagePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            GrowStyle = TableLayoutPanelGrowStyle.AddRows,
            ColumnCount = 3,
            Padding = new Padding(0, 0, 0, 8)
        };
        commitMessagePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
        commitMessagePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        commitMessagePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        AddFieldRow(commitMessagePanel, 0, "Commit message", _commitMessageBox);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, WrapContents = false };
        actions.Controls.Add(CreateButton("Save configuration", OnSaveConfiguration));
        actions.Controls.Add(CreateButton("Refresh status", OnRefreshStatus));
        actions.Controls.Add(CreateButton("Commit (local)", OnCommit));

        var pushButton = CreateButton("Push to GitHub", OnPush);
        pushButton.BackColor = Color.FromArgb(22, 163, 74);
        actions.Controls.Add(pushButton);

        _statusLabel.Dock = DockStyle.Top;
        _statusLabel.Padding = new Padding(0, 8, 0, 4);
        _statusLabel.Text = "Status: configure a repository first.";

        _autoSyncStatusLabel.Dock = DockStyle.Top;
        _autoSyncStatusLabel.Padding = new Padding(0, 2, 0, 8);
        _autoSyncStatusLabel.Text = "Auto-sync: Disabled";

        var changedFilesLabel = new Label
        {
            Text = "Changed files",
            Dock = DockStyle.Top,
            Height = 28,
            Font = new Font("Segoe UI Semibold", 11F),
            Padding = new Padding(0, 8, 0, 0)
        };
        _changedFilesList.Dock = DockStyle.Fill;
        _changedFilesList.Font = new Font("Consolas", 9F);

        var activityLabel = new Label
        {
            Text = "Git operations activity",
            Dock = DockStyle.Top,
            Height = 28,
            Font = new Font("Segoe UI Semibold", 11F),
            Padding = new Padding(0, 8, 0, 0)
        };
        _activityList.Dock = DockStyle.Fill;
        _activityList.Font = new Font("Consolas", 9F);

        var split = new SplitContainer
        {
            Dock = DockStyle.Top,
            Height = 360,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 190,
            Panel1MinSize = 150,
            Panel2MinSize = 120
        };

        var changedPanel = new Panel { Dock = DockStyle.Fill };
        changedPanel.Controls.Add(_changedFilesList);
        changedPanel.Controls.Add(changedFilesLabel);

        var activityPanel = new Panel { Dock = DockStyle.Fill };
        activityPanel.Controls.Add(_activityList);
        activityPanel.Controls.Add(activityLabel);

        split.Panel1.Controls.Add(changedPanel);
        split.Panel2.Controls.Add(activityPanel);

        var body = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        body.Controls.Add(split);
        body.Controls.Add(_autoSyncStatusLabel);
        body.Controls.Add(_statusLabel);
        body.Controls.Add(actions);
        body.Controls.Add(commitMessagePanel);
        body.Controls.Add(tokenNote);
        body.Controls.Add(form);

        Controls.Add(body);
        Controls.Add(subtitle);
        Controls.Add(title);
    }

    private async void OnLoad(object? sender, EventArgs e)
    {
        var config = await _gitHubService.GetConfigurationAsync();
        if (config is not null)
        {
            _localPathBox.Text = config.LocalPath;
            _remoteUrlBox.Text = config.RemoteUrl;
            _branchBox.Text = config.Branch;
            _templateBox.Text = config.CommitMessageTemplate;
            if (config.HasPersonalAccessToken)
            {
                _patBox.PlaceholderText = "Token already stored";
            }

            // Sync mode UI
            if (config.SyncMode == GitHubSyncMode.SmartAutoSync)
            {
                _syncModeCombo.SelectedItem = "Smart Auto Sync";
            }
            else
            {
                _syncModeCombo.SelectedItem = "Manual";
            }

            _inactivityCombo.SelectedIndex = config.InactivitySeconds switch
            {
                60 => 1,
                300 => 2,
                _ => 0
            };

            _autoSyncStatusLabel.Text = config.SyncMode == GitHubSyncMode.SmartAutoSync
                ? $"Auto-sync: Watching (inactivity {config.InactivitySeconds}s)"
                : "Auto-sync: Disabled";
        }

        await RefreshStatusAsync();
    }

    private void OnBrowseFolder(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Choose a local git repository folder",
            UseDescriptionForTitle = true,
            SelectedPath = _localPathBox.Text
        };

        if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
        {
            _localPathBox.Text = dialog.SelectedPath;
        }
    }

    private async void OnSaveConfiguration(object? sender, EventArgs e)
    {
        var token = _tokenEdited ? _patBox.Text : null;
        // Determine sync settings
        var selectedSync = _syncModeCombo.SelectedItem?.ToString() ?? "Manual";
        var syncMode = selectedSync switch
        {
            "Smart Auto Sync" => GitHubSyncMode.SmartAutoSync,
            _ => GitHubSyncMode.Manual
        };

        // Enforce Smart Auto Sync as a Premium feature at the UI level
        if (syncMode == GitHubSyncMode.SmartAutoSync && !_licenseService.IsPremium)
        {
            MessageBox.Show(
                FindForm(),
                "Smart Auto Sync is a Premium feature. Upgrade to Premium to enable it.",
                "Premium feature",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            // Revert to manual to avoid enabling the auto-sync feature
            syncMode = GitHubSyncMode.Manual;
            _syncModeCombo.SelectedItem = "Manual";
        }

        var inactivitySeconds = _inactivityCombo.SelectedIndex switch
        {
            1 => 60,
            2 => 300,
            _ => 30
        };

        var result = await _gitHubService.ConfigureRepositoryAsync(
            _localPathBox.Text,
            _remoteUrlBox.Text,
            _branchBox.Text,
            _templateBox.Text,
            token,
            syncMode,
            inactivitySeconds);

        ShowResult(result, "Configuration");
        if (result.Succeeded)
        {
            _patBox.Text = string.Empty;
            _tokenEdited = false;
            _patBox.PlaceholderText = "Token already stored";
            await RefreshStatusAsync();
        }
    }

    private async void OnRefreshStatus(object? sender, EventArgs e)
    {
        await RefreshStatusAsync();
    }

    private async void OnCommit(object? sender, EventArgs e)
    {
        var result = await _gitHubService.CommitAsync(_commitMessageBox.Text);
        ShowResult(result, "Commit");
        await RefreshStatusAsync();
    }

    private async void OnPush(object? sender, EventArgs e)
    {
        var confirm = MessageBox.Show(
            FindForm(),
            "This will push to GitHub. Continue?",
            "Confirm push",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes)
        {
            return;
        }

        var result = await _gitHubService.PushAsync();
        ShowResult(result, "Push");
        await RefreshStatusAsync();
    }

    private async Task RefreshStatusAsync()
    {
        var status = await _gitHubService.GetStatusAsync();
        _statusLabel.Text = $"Status: {status.Message}";

        _changedFilesList.Items.Clear();
        foreach (var file in status.ChangedFiles)
        {
            _changedFilesList.Items.Add(file);
        }
    }

    private void ShowResult(GitHubOperationResult result, string operation)
    {
        var prefix = result.Succeeded ? "OK" : "ERR";
        _activityList.Items.Insert(0, $"{DateTime.Now:HH:mm:ss}  [{prefix}]  {operation}: {result.Message}");
        while (_activityList.Items.Count > 100)
        {
            _activityList.Items.RemoveAt(_activityList.Items.Count - 1);
        }

        if (!result.Succeeded)
        {
            MessageBox.Show(
                FindForm(),
                result.Message,
                "GitHub Automation",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private static Button CreateButton(string text, EventHandler onClick)
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

    private static void AddFieldRow(
        TableLayoutPanel host,
        int row,
        string label,
        Control editor,
        Control? sideButton = null)
    {
        host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var caption = new Label
        {
            Text = label,
            AutoSize = true,
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
            Margin = new Padding(0, 7, 12, 4)
        };

        editor.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
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
            host.Controls.Add(new Panel { Width = 1 }, 2, row);
        }
    }
}
