using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Models;
using WindowsWorkflowAutomator.Services.Automation;
using WindowsWorkflowAutomator.Services.Scheduler;

namespace WindowsWorkflowAutomator.UI.Pages;

public sealed class TaskSchedulerPage : UserControl
{
    private readonly ISchedulerService _scheduler;
    private readonly IWorkflowService _workflows;
    private readonly IAppLogger _logger;
    private readonly DataGridView _grid = new();
    private List<ScheduledTask> _items = [];

    public TaskSchedulerPage(
        ISchedulerService scheduler,
        IWorkflowService workflows,
        IAppLogger logger)
    {
        _scheduler = scheduler;
        _workflows = workflows;
        _logger = logger;

        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(246, 248, 252);
        Padding = new Padding(24);

        BuildLayout();
        Load += async (_, _) => await RefreshAsync();
    }

    private void BuildLayout()
    {
        var title = new Label
        {
            Text = "Task Scheduler",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
            ForeColor = Color.FromArgb(17, 24, 39),
            Dock = DockStyle.Top
        };

        var subtitle = new Label
        {
            Text = "Schedule saved workflows to run once, every day, or every week.",
            AutoSize = true,
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.FromArgb(75, 85, 99),
            Padding = new Padding(0, 4, 0, 12),
            Dock = DockStyle.Top
        };

        var add = CreateButton("Add schedule", OnAdd);
        var edit = CreateButton("Edit", OnEdit);
        var delete = CreateButton("Delete", OnDelete);
        var run = CreateButton("Run now", OnRunNow);
        run.BackColor = Color.FromArgb(22, 163, 74);
        var refresh = CreateButton("Refresh", async (_, _) => await RefreshAsync());

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            WrapContents = false
        };
        buttons.Controls.Add(add);
        buttons.Controls.Add(edit);
        buttons.Controls.Add(delete);
        buttons.Controls.Add(run);
        buttons.Controls.Add(refresh);

        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.AutoGenerateColumns = false;
        _grid.BackgroundColor = Color.White;
        _grid.RowHeadersVisible = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "WorkflowName",
            HeaderText = "Workflow",
            FillWeight = 28
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Schedule",
            HeaderText = "Schedule",
            FillWeight = 22
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "ScheduledAt",
            HeaderText = "Date / Time",
            FillWeight = 25
        });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            DataPropertyName = nameof(ScheduledTask.IsEnabled),
            HeaderText = "Enabled",
            FillWeight = 12
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "LastRun",
            HeaderText = "Last Run",
            FillWeight = 18
        });

        Controls.Add(_grid);
        Controls.Add(buttons);
        Controls.Add(subtitle);
        Controls.Add(title);
    }

    private async Task RefreshAsync()
    {
        _items = [.. await _scheduler.GetAllAsync()];
        _grid.DataSource = _items.Select(x => new ScheduleRow
        {
            Id = x.Id,
            WorkflowName = x.Workflow?.Name ?? $"Workflow #{x.WorkflowId}",
            Schedule = FormatSchedule(x),
            ScheduledAt = x.ScheduledAt.ToString("yyyy-MM-dd HH:mm"),
            IsEnabled = x.IsEnabled,
            LastRun = x.LastRunAtUtc.HasValue
                ? x.LastRunAtUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                : "Never"
        }).ToList();
    }

    private async void OnAdd(object? sender, EventArgs e)
    {
        var workflows = await _workflows.GetAllAsync();
        if (workflows.Count == 0)
        {
            ShowError("Create at least one workflow before adding a schedule.");
            return;
        }

        using var dialog = new ScheduleEditDialog(workflows, null);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
            return;

        try
        {
            await _scheduler.CreateAsync(dialog.ToTask());
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async void OnEdit(object? sender, EventArgs e)
    {
        var selected = GetSelected();
        if (selected is null)
        {
            ShowError("Select a schedule first.");
            return;
        }

        var workflows = await _workflows.GetAllAsync();
        using var dialog = new ScheduleEditDialog(workflows, selected);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
            return;

        try
        {
            await _scheduler.UpdateAsync(dialog.ToTask(selected));
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async void OnDelete(object? sender, EventArgs e)
    {
        var selected = GetSelected();
        if (selected is null)
        {
            ShowError("Select a schedule first.");
            return;
        }

        var confirm = MessageBox.Show(
            FindForm(),
            $"Delete schedule #{selected.Id}?",
            "Delete schedule",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
            return;

        try
        {
            await _scheduler.DeleteAsync(selected.Id);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async void OnRunNow(object? sender, EventArgs e)
    {
        var selected = GetSelected();
        if (selected is null)
        {
            ShowError("Select a schedule first.");
            return;
        }

        try
        {
            var result = await _scheduler.RunNowAsync(selected.Id);
            var details = result.Steps.Count == 0
                ? result.Summary
                : string.Join(Environment.NewLine, result.Steps.Select(x => x.Message));

            MessageBox.Show(
                FindForm(),
                result.Summary + Environment.NewLine + Environment.NewLine + details,
                result.Succeeded ? "Workflow finished" : "Workflow finished with errors",
                MessageBoxButtons.OK,
                result.Succeeded ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.Error("Manual scheduled workflow run failed.", ex);
            ShowError(ex.Message);
        }
    }

    private ScheduledTask? GetSelected()
    {
        if (_grid.CurrentRow?.DataBoundItem is not ScheduleRow row)
            return null;

        return _items.FirstOrDefault(x => x.Id == row.Id);
    }

    private static string FormatSchedule(ScheduledTask task) =>
        task.ScheduleType switch
        {
            ScheduleType.OneTime => "One time",
            ScheduleType.Daily => "Daily",
            ScheduleType.Weekly => $"Weekly ({task.WeeklyDay})",
            _ => task.ScheduleType.ToString()
        };

    private void ShowError(string message) =>
        MessageBox.Show(FindForm(), message, "Task Scheduler", MessageBoxButtons.OK, MessageBoxIcon.Warning);

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

    private sealed class ScheduleRow
    {
        public int Id { get; init; }
        public string WorkflowName { get; init; } = string.Empty;
        public string Schedule { get; init; } = string.Empty;
        public string ScheduledAt { get; init; } = string.Empty;
        public bool IsEnabled { get; init; }
        public string LastRun { get; init; } = string.Empty;
    }
}
