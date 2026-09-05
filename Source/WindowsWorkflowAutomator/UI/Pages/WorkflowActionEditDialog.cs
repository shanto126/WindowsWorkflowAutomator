using WindowsWorkflowAutomator.Models;
using WindowsWorkflowAutomator.UI;

namespace WindowsWorkflowAutomator.UI.Pages;

internal sealed class WorkflowActionEditDialog : Form
{
    private readonly ComboBox _typeBox = new();
    private readonly TextBox _targetBox = new();
    private readonly TextBox _argumentsBox = new();

    public WorkflowActionEditDialog(WorkflowAction? existing)
    {
        Text = existing is null ? "Add action" : "Edit action";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(560, 200);
        Font = new Font("Segoe UI", 9.5F);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 4,
            Padding = new Padding(16)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));

        _typeBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _typeBox.Items.AddRange(Enum.GetNames<WorkflowActionType>());
        _typeBox.SelectedItem = (existing?.Type ?? WorkflowActionType.OpenApplication).ToString();
        _typeBox.Dock = DockStyle.Fill;
        _typeBox.SelectedIndexChanged += (_, _) => UpdateTargetHint();

        _targetBox.Dock = DockStyle.Fill;
        _argumentsBox.Dock = DockStyle.Fill;
        _targetBox.Text = existing?.Target ?? string.Empty;
        _argumentsBox.Text = existing?.Arguments ?? string.Empty;

        var browse = new Button { Text = "Browse", Dock = DockStyle.Fill };
        browse.Click += (_, _) => BrowseTarget();

        layout.Controls.Add(new Label { Text = "Type", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        layout.Controls.Add(_typeBox, 1, 0);
        layout.SetColumnSpan(_typeBox, 2);
        layout.Controls.Add(new Label { Text = "Target", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        layout.Controls.Add(_targetBox, 1, 1);
        layout.Controls.Add(browse, 2, 1);
        layout.Controls.Add(new Label { Text = "Arguments", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
        layout.Controls.Add(_argumentsBox, 1, 2);
        layout.SetColumnSpan(_argumentsBox, 2);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        var ok = new Button { Text = "Save", DialogResult = DialogResult.OK, AutoSize = true };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        layout.Controls.Add(buttons, 0, 3);
        layout.SetColumnSpan(buttons, 3);

        AcceptButton = ok;
        CancelButton = cancel;
        Controls.Add(layout);
        UpdateTargetHint();
    }

    public WorkflowAction ToAction(int order)
    {
        return new WorkflowAction
        {
            Type = Enum.Parse<WorkflowActionType>(_typeBox.SelectedItem?.ToString() ?? nameof(WorkflowActionType.OpenApplication)),
            Target = _targetBox.Text.Trim(),
            Arguments = string.IsNullOrWhiteSpace(_argumentsBox.Text) ? null : _argumentsBox.Text.Trim(),
            Order = order
        };
    }

    private WorkflowActionType SelectedType =>
        Enum.Parse<WorkflowActionType>(_typeBox.SelectedItem?.ToString() ?? nameof(WorkflowActionType.OpenApplication));

    private void UpdateTargetHint()
    {
        _targetBox.PlaceholderText = SelectedType switch
        {
            WorkflowActionType.OpenApplication => @"C:\Windows\System32\notepad.exe",
            WorkflowActionType.OpenWebsite => "https://example.com",
            WorkflowActionType.OpenFolder => @"C:\Users",
            _ => string.Empty
        };
        _argumentsBox.Enabled = SelectedType == WorkflowActionType.OpenApplication;
    }

    private async void BrowseTarget()
    {
        if (SelectedType == WorkflowActionType.OpenWebsite)
        {
            return;
        }

        if (SelectedType == WorkflowActionType.OpenFolder)
        {
            var folderPath = await FileDialogService.SelectFolderAsync(
                "Choose a folder",
                _targetBox.Text,
                SynchronizationContext.Current);
            if (folderPath is not null)
            {
                _targetBox.Text = folderPath;
            }

            return;
        }

        var filePaths = await FileDialogService.SelectFilesAsync(
            "Select application",
            "Programs (*.exe)|*.exe|All files (*.*)|*.*",
            Path.GetDirectoryName(_targetBox.Text),
            multiselect: false,
            SynchronizationContext.Current);
        if (filePaths is not null)
        {
            _targetBox.Text = filePaths[0];
        }
    }
}
