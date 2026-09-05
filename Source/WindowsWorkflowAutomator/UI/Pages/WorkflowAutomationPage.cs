using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Models;
using WindowsWorkflowAutomator.Services.Automation;
using WindowsWorkflowAutomator.UI;

namespace WindowsWorkflowAutomator.UI.Pages;

public sealed class WorkflowAutomationPage : UserControl
{
    private readonly IWorkflowService _workflows;
    private readonly IAppLogger _logger;
    private readonly ListBox _workflowList = new();
    private readonly Label _detailTitle = new();
    private readonly Label _detailDescription = new();
    private readonly CheckBox _enabledBox = new();
    private readonly DataGridView _actionsGrid = new();
    private readonly TextBox _runLog = new();
    private readonly BindingSource _actionBinding = new();
    private List<Workflow> _items = [];
    private Workflow? _selected;

    public WorkflowAutomationPage(IWorkflowService workflows, IAppLogger logger)
    {
        _workflows = workflows;
        _logger = logger;
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(246, 248, 252);
        Padding = new Padding(24);
        BuildLayout();
        Load += async (_, _) => await RefreshListAsync();
    }

    private void BuildLayout()
    {
        var title = new Label
        {
            Text = "Workflow Automation",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
            ForeColor = Color.FromArgb(17, 24, 39),
            Dock = DockStyle.Top
        };
        var subtitle = new Label
        {
            Text = "Save a sequence of actions (app, website, folder) and run them in order.",
            AutoSize = true,
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.FromArgb(75, 85, 99),
            Padding = new Padding(0, 4, 0, 12),
            Dock = DockStyle.Top
        };

        var addWorkflow = CreateButton("Add workflow", OnAddWorkflow);
        var editWorkflow = CreateButton("Edit", OnEditWorkflow);
        var deleteWorkflow = CreateButton("Delete", OnDeleteWorkflow);
        var runNow = CreateButton("Run now", OnRunNow);
        runNow.ApplyPrimaryStyle();

        var listButtons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, WrapContents = false };
        listButtons.Controls.Add(addWorkflow);
        listButtons.Controls.Add(editWorkflow);
        listButtons.Controls.Add(deleteWorkflow);
        listButtons.Controls.Add(runNow);

        _workflowList.Dock = DockStyle.Fill;
        _workflowList.DisplayMember = nameof(Workflow.Name);
        _workflowList.SelectedIndexChanged += async (_, _) => await ShowSelectedAsync();

        var left = new Panel { Dock = DockStyle.Left, Width = 280, Padding = new Padding(0, 0, 16, 0) };
        left.Controls.Add(_workflowList);
        left.Controls.Add(listButtons);

        _detailTitle.Font = new Font("Segoe UI Semibold", 14F);
        _detailTitle.AutoSize = true;
        _detailTitle.Dock = DockStyle.Top;
        _detailDescription.AutoSize = true;
        _detailDescription.MaximumSize = new Size(700, 0);
        _detailDescription.ForeColor = Color.FromArgb(75, 85, 99);
        _detailDescription.Dock = DockStyle.Top;
        _enabledBox.Text = "Enabled";
        _enabledBox.AutoSize = true;
        _enabledBox.Dock = DockStyle.Top;
        _enabledBox.CheckedChanged += OnEnabledChanged;

        var addAction = CreateButton("Add action", OnAddAction);
        var editAction = CreateButton("Edit action", OnEditAction);
        var removeAction = CreateButton("Remove", OnRemoveAction);
        var moveUp = CreateButton("Move up", (_, _) => MoveAction(-1));
        var moveDown = CreateButton("Move down", (_, _) => MoveAction(1));
        var actionButtons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, WrapContents = false };
        actionButtons.Controls.Add(addAction);
        actionButtons.Controls.Add(editAction);
        actionButtons.Controls.Add(removeAction);
        actionButtons.Controls.Add(moveUp);
        actionButtons.Controls.Add(moveDown);

        _actionsGrid.Dock = DockStyle.Fill;
        _actionsGrid.ReadOnly = true;
        _actionsGrid.AllowUserToAddRows = false;
        _actionsGrid.AllowUserToDeleteRows = false;
        _actionsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _actionsGrid.MultiSelect = false;
        _actionsGrid.AutoGenerateColumns = false;
        _actionsGrid.BackgroundColor = Color.White;
        _actionsGrid.RowHeadersVisible = false;
        _actionsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _actionsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(WorkflowAction.Order), HeaderText = "#", FillWeight = 10 });
        _actionsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(WorkflowAction.Type), HeaderText = "Type", FillWeight = 22 });
        _actionsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(WorkflowAction.Target), HeaderText = "Target", FillWeight = 48 });
        _actionsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(WorkflowAction.Arguments), HeaderText = "Args", FillWeight = 20 });
        _actionsGrid.ApplyModernStyle();
        _actionsGrid.DataSource = _actionBinding;

        _runLog.Dock = DockStyle.Bottom;
        _runLog.Height = 120;
        _runLog.Multiline = true;
        _runLog.ReadOnly = true;
        _runLog.ScrollBars = ScrollBars.Vertical;
        _runLog.Font = new Font("Consolas", 9F);
        _runLog.PlaceholderText = "Run results appear here.";

        var right = new Panel { Dock = DockStyle.Fill };
        var gridHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 8) };
        gridHost.Controls.Add(_actionsGrid);
        right.Controls.Add(gridHost);
        right.Controls.Add(_runLog);
        right.Controls.Add(actionButtons);
        right.Controls.Add(_enabledBox);
        right.Controls.Add(_detailDescription);
        right.Controls.Add(_detailTitle);

        var body = new Panel { Dock = DockStyle.Fill };
        body.Controls.Add(right);
        body.Controls.Add(left);

        Controls.Add(body);
        Controls.Add(subtitle);
        Controls.Add(title);
    }

    private async Task RefreshListAsync(int? selectId = null)
    {
        _items = [.. await _workflows.GetAllAsync()];
        _workflowList.DataSource = null;
        _workflowList.DataSource = _items;
        _workflowList.DisplayMember = nameof(Workflow.Name);
        if (_items.Count == 0)
        {
            _selected = null;
            BindDetails();
            return;
        }

        var id = selectId ?? _selected?.Id ?? _items[0].Id;
        var index = _items.FindIndex(x => x.Id == id);
        _workflowList.SelectedIndex = index >= 0 ? index : 0;
        await ShowSelectedAsync();
    }

    private async Task ShowSelectedAsync()
    {
        if (_workflowList.SelectedItem is not Workflow listed)
        {
            _selected = null;
            BindDetails();
            return;
        }

        _selected = await _workflows.GetByIdAsync(listed.Id) ?? listed;
        BindDetails();
    }

    private void BindDetails()
    {
        _enabledBox.CheckedChanged -= OnEnabledChanged;
        if (_selected is null)
        {
            _detailTitle.Text = "No workflow selected";
            _detailDescription.Text = "Add a workflow to get started.";
            _enabledBox.Checked = false;
            _actionBinding.DataSource = new List<WorkflowAction>();
        }
        else
        {
            _detailTitle.Text = _selected.Name;
            _detailDescription.Text = string.IsNullOrWhiteSpace(_selected.Description)
                ? "No description"
                : _selected.Description;
            _enabledBox.Checked = _selected.IsEnabled;
            _selected.Actions = [.. _selected.Actions.OrderBy(a => a.Order).ThenBy(a => a.Id)];
            _actionBinding.DataSource = _selected.Actions;
        }

        _actionBinding.ResetBindings(false);
        _enabledBox.CheckedChanged += OnEnabledChanged;
    }

    private async void OnAddWorkflow(object? sender, EventArgs e)
    {
        var workflow = new Workflow();
        using var dialog = new WorkflowEditDialog(null);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        dialog.ApplyTo(workflow);
        try
        {
            var created = await _workflows.CreateAsync(workflow);
            await RefreshListAsync(created.Id);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async void OnEditWorkflow(object? sender, EventArgs e)
    {
        if (_selected is null)
        {
            ShowError("Select a workflow first.");
            return;
        }

        using var dialog = new WorkflowEditDialog(_selected);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        dialog.ApplyTo(_selected);
        try
        {
            await _workflows.UpdateAsync(_selected);
            await RefreshListAsync(_selected.Id);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async void OnDeleteWorkflow(object? sender, EventArgs e)
    {
        if (_selected is null)
        {
            ShowError("Select a workflow first.");
            return;
        }

        var confirm = MessageBox.Show(
            FindForm(),
            $"Delete workflow '{_selected.Name}'?",
            "Delete workflow",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        await _workflows.DeleteAsync(_selected.Id);
        _selected = null;
        await RefreshListAsync();
    }

    private async void OnEnabledChanged(object? sender, EventArgs e)
    {
        if (_selected is null)
        {
            return;
        }

        try
        {
            await _workflows.SetEnabledAsync(_selected.Id, _enabledBox.Checked);
            _selected.IsEnabled = _enabledBox.Checked;
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async void OnAddAction(object? sender, EventArgs e)
    {
        if (_selected is null)
        {
            ShowError("Select a workflow first.");
            return;
        }

        using var dialog = new WorkflowActionEditDialog(null);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        _selected.Actions.Add(dialog.ToAction(_selected.Actions.Count));
        await SaveSelectedActionsAsync();
    }

    private async void OnEditAction(object? sender, EventArgs e)
    {
        if (_selected is null)
        {
            return;
        }

        var current = GetSelectedAction();
        if (current is null)
        {
            ShowError("Select an action to edit.");
            return;
        }

        using var dialog = new WorkflowActionEditDialog(current);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        var index = _selected.Actions.IndexOf(current);
        var updated = dialog.ToAction(current.Order);
        updated.Id = current.Id;
        _selected.Actions[index] = updated;
        await SaveSelectedActionsAsync();
    }

    private async void OnRemoveAction(object? sender, EventArgs e)
    {
        if (_selected is null)
        {
            return;
        }

        var current = GetSelectedAction();
        if (current is null)
        {
            ShowError("Select an action to remove.");
            return;
        }

        _selected.Actions.Remove(current);
        await SaveSelectedActionsAsync();
    }

    private async void MoveAction(int delta)
    {
        if (_selected is null)
        {
            return;
        }

        var ordered = _selected.Actions.OrderBy(a => a.Order).ToList();
        var current = GetSelectedAction();
        if (current is null)
        {
            return;
        }

        var index = ordered.IndexOf(current);
        var next = index + delta;
        if (index < 0 || next < 0 || next >= ordered.Count)
        {
            return;
        }

        (ordered[index], ordered[next]) = (ordered[next], ordered[index]);
        _selected.Actions = ordered;
        await SaveSelectedActionsAsync();
    }

    private async Task SaveSelectedActionsAsync()
    {
        if (_selected is null)
        {
            return;
        }

        try
        {
            await _workflows.UpdateAsync(_selected);
            await RefreshListAsync(_selected.Id);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
            await RefreshListAsync(_selected.Id);
        }
    }

    private async void OnRunNow(object? sender, EventArgs e)
    {
        if (_selected is null)
        {
            ShowError("Select a workflow first.");
            return;
        }

        try
        {
            var result = await _workflows.RunAsync(_selected.Id);
            _runLog.Text = string.Join(Environment.NewLine, result.Steps.Select(s => s.Message));
            if (result.Steps.Count == 0)
            {
                _runLog.Text = result.Summary;
            }

            MessageBox.Show(
                FindForm(),
                result.Summary + Environment.NewLine + Environment.NewLine +
                string.Join(Environment.NewLine, result.Steps.Select(s => s.Message)),
                result.Succeeded ? "Workflow finished" : "Workflow finished with errors",
                MessageBoxButtons.OK,
                result.Succeeded ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            _logger.Error("Workflow run failed.", ex);
            ShowError(ex.Message);
        }
    }

    private WorkflowAction? GetSelectedAction() =>
        _actionsGrid.CurrentRow?.DataBoundItem as WorkflowAction;

    private void ShowError(string message) =>
        MessageBox.Show(FindForm(), message, "Workflow Automation", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    private static Button CreateButton(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 32,
            Margin = new Padding(0, 0, 8, 0),
            FlatStyle = FlatStyle.Flat
        };
        button.ApplyPrimaryStyle();
        button.Click += onClick;
        return button;
    }
}
