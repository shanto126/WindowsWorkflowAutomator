using WindowsWorkflowAutomator.Configuration;
using WindowsWorkflowAutomator.FileOrganizer;
using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.UI.Pages;

public class FileOrganizerPage : UserControl
{
    private readonly IFileRuleService _rules;
    private readonly IFileOrganizerService _organizer;
    private readonly IDownloadFolderMonitor _monitor;
    private readonly IAppSettingsService _settings;
    private readonly IAppLogger _logger;

    private readonly TextBox _folderBox = new();
    private readonly CheckBox _monitorToggle = new();
    private readonly DataGridView _rulesGrid = new();
    private readonly ListBox _activityList = new();
    private readonly BindingSource _ruleBinding = new();
    private List<FileOrganizationRule> _ruleItems = [];

    public FileOrganizerPage(
        IFileRuleService rules,
        IFileOrganizerService organizer,
        IDownloadFolderMonitor monitor,
        IAppSettingsService settings,
        IAppLogger logger)
    {
        _rules = rules;
        _organizer = organizer;
        _monitor = monitor;
        _settings = settings;
        _logger = logger;

        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(246, 248, 252);
        Padding = new Padding(24);
        BuildLayout();
        Load += OnLoad;
        Disposed += OnDisposed;
    }

    private void BuildLayout()
    {
        var title = new Label
        {
            Text = "File Organizer",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
            ForeColor = Color.FromArgb(17, 24, 39)
        };

        var description = new Label
        {
            Text = "Watch a folder, match files by extension, and move, copy, or rename them using rules.",
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.FromArgb(75, 85, 99),
            Padding = new Padding(0, 4, 0, 12)
        };

        _folderBox.ReadOnly = true;
        _folderBox.Dock = DockStyle.Fill;

        var browse = CreateButton("Browse…", OnBrowseFolder);
        var organizeNow = CreateButton("Organize now", OnOrganizeNow);
        _monitorToggle.Text = "Enable monitoring";
        _monitorToggle.AutoSize = true;
        _monitorToggle.Padding = new Padding(12, 6, 0, 0);
        _monitorToggle.CheckedChanged += OnMonitorToggled;

        var folderRow = new TableLayoutPanel { Dock = DockStyle.Top, Height = 36, ColumnCount = 4 };
        folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        folderRow.Controls.Add(_folderBox, 0, 0);
        folderRow.Controls.Add(browse, 1, 0);
        folderRow.Controls.Add(organizeNow, 2, 0);
        folderRow.Controls.Add(_monitorToggle, 3, 0);

        var add = CreateButton("Add rule", OnAddRule);
        var edit = CreateButton("Edit rule", OnEditRule);
        var delete = CreateButton("Delete rule", OnDeleteRule);
        var ruleButtons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, WrapContents = false };
        ruleButtons.Controls.Add(add);
        ruleButtons.Controls.Add(edit);
        ruleButtons.Controls.Add(delete);

        _rulesGrid.Dock = DockStyle.Fill;
        _rulesGrid.ReadOnly = true;
        _rulesGrid.AllowUserToAddRows = false;
        _rulesGrid.AllowUserToDeleteRows = false;
        _rulesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _rulesGrid.MultiSelect = false;
        _rulesGrid.AutoGenerateColumns = false;
        _rulesGrid.BackgroundColor = Color.White;
        _rulesGrid.RowHeadersVisible = false;
        _rulesGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _rulesGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(FileOrganizationRule.Id), HeaderText = "Id", FillWeight = 12 });
        _rulesGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(FileOrganizationRule.Extension), HeaderText = "Extension", FillWeight = 22 });
        _rulesGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(FileOrganizationRule.Action), HeaderText = "Action", FillWeight = 16 });
        _rulesGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(FileOrganizationRule.DestinationFolder), HeaderText = "Destination", FillWeight = 36 });
        _rulesGrid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(FileOrganizationRule.IsEnabled), HeaderText = "On", FillWeight = 10 });
        _rulesGrid.DataSource = _ruleBinding;

        var activityLabel = new Label
        {
            Text = "Recent activity",
            Dock = DockStyle.Top,
            Height = 28,
            Font = new Font("Segoe UI Semibold", 11F),
            Padding = new Padding(0, 8, 0, 0)
        };
        _activityList.Dock = DockStyle.Fill;
        _activityList.Font = new Font("Consolas", 9F);

        var rulesHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 8) };
        rulesHost.Controls.Add(_rulesGrid);
        rulesHost.Controls.Add(ruleButtons);

        var activityHost = new Panel { Dock = DockStyle.Bottom, Height = 180 };
        activityHost.Controls.Add(_activityList);
        activityHost.Controls.Add(activityLabel);

        var root = new Panel { Dock = DockStyle.Fill };
        root.Controls.Add(rulesHost);
        root.Controls.Add(activityHost);
        root.Controls.Add(folderRow);
        root.Controls.Add(description);
        root.Controls.Add(title);
        title.Dock = DockStyle.Top;
        description.Dock = DockStyle.Top;
        Controls.Add(root);
    }

    private async void OnLoad(object? sender, EventArgs e)
    {
        _organizer.ActionRecorded += OnActionRecorded;
        _folderBox.Text = string.IsNullOrWhiteSpace(_settings.Current.FileOrganizerWatchFolder)
            ? GetDefaultDownloadsFolder()
            : _settings.Current.FileOrganizerWatchFolder;

        foreach (var entry in _organizer.RecentActions.Reverse())
        {
            _activityList.Items.Add(entry.ToString());
        }

        await RefreshRulesAsync();

        _monitorToggle.Checked = _settings.Current.FileOrganizerMonitoringEnabled;
        if (_monitorToggle.Checked && !_monitor.IsRunning)
        {
            TryStartMonitor();
        }
    }

    private void OnDisposed(object? sender, EventArgs e)
    {
        _organizer.ActionRecorded -= OnActionRecorded;
    }

    private void OnActionRecorded(object? sender, FileOrganizationActionLog entry)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        BeginInvoke(new Action(() =>
        {
            _activityList.Items.Insert(0, entry.ToString());
            while (_activityList.Items.Count > 100)
            {
                _activityList.Items.RemoveAt(_activityList.Items.Count - 1);
            }
        }));
    }

    private async Task RefreshRulesAsync()
    {
        _ruleItems = [.. await _rules.GetAllAsync()];
        _ruleBinding.DataSource = _ruleItems;
        _ruleBinding.ResetBindings(false);
    }

    private async void OnAddRule(object? sender, EventArgs e)
    {
        using var dialog = new FileOrganizerRuleDialog(null);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        try
        {
            await _rules.AddAsync(dialog.ToRule(0));
            await RefreshRulesAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async void OnEditRule(object? sender, EventArgs e)
    {
        var selected = GetSelectedRule();
        if (selected is null)
        {
            ShowError("Select a rule to edit.");
            return;
        }

        using var dialog = new FileOrganizerRuleDialog(selected);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        try
        {
            await _rules.UpdateAsync(dialog.ToRule(selected.Id));
            await RefreshRulesAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async void OnDeleteRule(object? sender, EventArgs e)
    {
        var selected = GetSelectedRule();
        if (selected is null)
        {
            ShowError("Select a rule to delete.");
            return;
        }

        var confirm = MessageBox.Show(
            FindForm(),
            $"Delete the rule for {selected.Extension}?",
            "Delete rule",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        await _rules.DeleteAsync(selected.Id);
        await RefreshRulesAsync();
    }

    private void OnBrowseFolder(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Choose the folder to watch",
            UseDescriptionForTitle = true,
            SelectedPath = _folderBox.Text
        };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        _folderBox.Text = dialog.SelectedPath;
        _settings.Current.FileOrganizerWatchFolder = dialog.SelectedPath;
        _settings.Save();
        if (_monitorToggle.Checked)
        {
            TryStartMonitor();
        }
    }

    private async void OnOrganizeNow(object? sender, EventArgs e)
    {
        var folder = _folderBox.Text;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            ShowError("Choose a valid folder first.");
            return;
        }

        try
        {
            await _organizer.OrganizeFolderAsync(folder);
        }
        catch (Exception ex)
        {
            _logger.Error("Manual organize failed.", ex);
            ShowError(ex.Message);
        }
    }

    private void OnMonitorToggled(object? sender, EventArgs e)
    {
        _settings.Current.FileOrganizerMonitoringEnabled = _monitorToggle.Checked;
        _settings.Current.FileOrganizerWatchFolder = _folderBox.Text;
        _settings.Save();

        if (_monitorToggle.Checked)
        {
            TryStartMonitor();
        }
        else
        {
            _monitor.Stop();
            AppendActivity("Monitoring stopped.");
        }
    }

    private void TryStartMonitor()
    {
        try
        {
            _monitor.Start(_folderBox.Text);
            AppendActivity($"Monitoring started: {_folderBox.Text}");
        }
        catch (Exception ex)
        {
            _monitorToggle.Checked = false;
            _settings.Current.FileOrganizerMonitoringEnabled = false;
            _settings.Save();
            ShowError(ex.Message);
        }
    }

    private FileOrganizationRule? GetSelectedRule()
    {
        if (_rulesGrid.CurrentRow?.DataBoundItem is FileOrganizationRule rule)
        {
            return rule;
        }

        return null;
    }

    private void AppendActivity(string message)
    {
        _activityList.Items.Insert(0, $"{DateTime.Now:HH:mm:ss}  [INFO]  {message}");
    }

    private void ShowError(string message) =>
        MessageBox.Show(FindForm(), message, "File Organizer", MessageBoxButtons.OK, MessageBoxIcon.Warning);

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

    private static string GetDefaultDownloadsFolder()
    {
        var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        return Directory.Exists(downloads) ? downloads : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }
}
