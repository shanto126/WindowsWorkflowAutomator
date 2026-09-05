using WindowsWorkflowAutomator.Models;
using WindowsWorkflowAutomator.Repositories;
using WindowsWorkflowAutomator.UI;

namespace WindowsWorkflowAutomator.UI.Pages;

public sealed class ActivityLogsPage : UserControl
{
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly DataGridView _grid = new();

    public ActivityLogsPage(IActivityLogRepository activityLogRepository)
    {
        _activityLogRepository = activityLogRepository;

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
            Text = "Activity Logs",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
            ForeColor = Color.FromArgb(17, 24, 39),
            Dock = DockStyle.Top
        };

        var subtitle = new Label
        {
            Text = "Recent application activity and system events.",
            AutoSize = true,
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.FromArgb(75, 85, 99),
            Padding = new Padding(0, 4, 0, 12),
            Dock = DockStyle.Top
        };

        var refreshButton = new Button
        {
            Text = "Refresh",
            AutoSize = true,
            Height = 32,
            Margin = new Padding(0, 0, 0, 8),
        };
        refreshButton.ApplyPrimaryStyle();
        refreshButton.Click += async (_, _) => await RefreshAsync();

        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoGenerateColumns = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.BackgroundColor = Color.White;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ActivityLogEntry.TimestampUtc), HeaderText = "Timestamp", FillWeight = 18 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ActivityLogEntry.Level), HeaderText = "Level", FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ActivityLogEntry.Category), HeaderText = "Category", FillWeight = 16 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ActivityLogEntry.Message), HeaderText = "Message", FillWeight = 56 });
        _grid.ApplyModernStyle();

        var topBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            WrapContents = false
        };
        topBar.Controls.Add(refreshButton);

        var body = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        body.Controls.Add(_grid);
        body.Controls.Add(topBar);

        Controls.Add(body);
        Controls.Add(subtitle);
        Controls.Add(title);
    }

    private async void OnLoad(object? sender, EventArgs e) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        try
        {
            var entries = await _activityLogRepository.GetRecentAsync(200);
            _grid.DataSource = entries
                .OrderByDescending(x => x.TimestampUtc)
                .Select(x => new
                {
                    x.TimestampUtc,
                    x.Level,
                    x.Category,
                    x.Message
                })
                .ToList();
        }
        catch (Exception)
        {
            _grid.DataSource = new List<ActivityLogEntry>();
        }
    }
}
