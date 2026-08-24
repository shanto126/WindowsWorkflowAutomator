using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.UI.Pages;

internal sealed class ScheduleEditDialog : Form
{
    private readonly ComboBox _workflowBox = new();
    private readonly ComboBox _typeBox = new();
    private readonly DateTimePicker _dateTimePicker = new();
    private readonly ComboBox _dayBox = new();
    private readonly CheckBox _enabledBox = new();

    private readonly IReadOnlyList<Workflow> _workflows;

    public ScheduleEditDialog(IReadOnlyList<Workflow> workflows, ScheduledTask? existing)
    {
        _workflows = workflows;
        Text = existing is null ? "Add schedule" : "Edit schedule";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(500, 280);
        Font = new Font("Segoe UI", 9.5F);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(16)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _workflowBox.Dock = DockStyle.Fill;
        _workflowBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _workflowBox.DataSource = _workflows.ToList();
        _workflowBox.DisplayMember = nameof(Workflow.Name);
        _workflowBox.ValueMember = nameof(Workflow.Id);

        _typeBox.Dock = DockStyle.Fill;
        _typeBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _typeBox.DataSource = Enum.GetValues<ScheduleType>();

        _dateTimePicker.Dock = DockStyle.Fill;
        _dateTimePicker.Format = DateTimePickerFormat.Custom;
        _dateTimePicker.CustomFormat = "yyyy-MM-dd HH:mm";
        _dateTimePicker.ShowUpDown = false;

        _dayBox.Dock = DockStyle.Fill;
        _dayBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _dayBox.DataSource = Enum.GetValues<DayOfWeek>();
        _dayBox.Visible = false;

        _enabledBox.Text = "Enabled";
        _enabledBox.AutoSize = true;

        layout.Controls.Add(new Label { Text = "Workflow", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        layout.Controls.Add(_workflowBox, 1, 0);
        layout.Controls.Add(new Label { Text = "Repeat", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        layout.Controls.Add(_typeBox, 1, 1);
        layout.Controls.Add(new Label { Text = "Date / time", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
        layout.Controls.Add(_dateTimePicker, 1, 2);
        layout.Controls.Add(new Label { Text = "Weekday", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
        layout.Controls.Add(_dayBox, 1, 3);
        layout.Controls.Add(_enabledBox, 1, 4);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        var ok = new Button { Text = "Save", DialogResult = DialogResult.OK, AutoSize = true };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        layout.Controls.Add(buttons, 0, 5);
        layout.SetColumnSpan(buttons, 2);

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        _typeBox.SelectedIndexChanged += (_, _) => UpdateTypeVisibility();

        if (existing is null)
        {
            _dateTimePicker.Value = DateTime.Now.AddMinutes(5);
            _enabledBox.Checked = true;
            _typeBox.SelectedItem = ScheduleType.OneTime;
        }
        else
        {
            _workflowBox.SelectedValue = existing.WorkflowId;
            _typeBox.SelectedItem = existing.ScheduleType;
            _dateTimePicker.Value = existing.ScheduledAt < DateTime.Now
                ? DateTime.Now.AddMinutes(1)
                : existing.ScheduledAt;
            _dayBox.SelectedItem = existing.WeeklyDay ?? DayOfWeek.Monday;
            _enabledBox.Checked = existing.IsEnabled;
        }

        UpdateTypeVisibility();
        AcceptButton = ok;
        CancelButton = cancel;
        Controls.Add(layout);
    }

    private void UpdateTypeVisibility()
    {
        var weekly = _typeBox.SelectedItem is ScheduleType.Weekly;
        _dayBox.Visible = weekly;
    }

    public ScheduledTask ToTask(ScheduledTask? existing = null)
    {
        var task = existing ?? new ScheduledTask();
        task.WorkflowId = Convert.ToInt32(_workflowBox.SelectedValue);
        task.ScheduleType = (ScheduleType)_typeBox.SelectedItem!;
        task.ScheduledAt = _dateTimePicker.Value;
        task.WeeklyDay = task.ScheduleType == ScheduleType.Weekly
            ? (DayOfWeek)_dayBox.SelectedItem!
            : null;
        task.IsEnabled = _enabledBox.Checked;
        return task;
    }
}
