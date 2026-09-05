using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.UI.Pages;

internal sealed class WorkflowEditDialog : Form
{
    private readonly TextBox _nameBox = new();
    private readonly TextBox _descriptionBox = new();
    private readonly CheckBox _enabledBox = new();

    public WorkflowEditDialog(Workflow? existing)
    {
        Text = existing is null ? "Add workflow" : "Edit workflow";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(460, 220);
        Font = new Font("Segoe UI", 9.5F);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(16)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _nameBox.Dock = DockStyle.Fill;
        _descriptionBox.Dock = DockStyle.Fill;
        _descriptionBox.Multiline = true;
        _nameBox.Text = existing?.Name ?? string.Empty;
        _descriptionBox.Text = existing?.Description ?? string.Empty;
        _enabledBox.Text = "Enabled";
        _enabledBox.Checked = existing?.IsEnabled ?? true;
        _enabledBox.AutoSize = true;

        layout.Controls.Add(new Label { Text = "Name", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        layout.Controls.Add(_nameBox, 1, 0);
        layout.Controls.Add(new Label { Text = "Description", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        layout.Controls.Add(_descriptionBox, 1, 1);
        layout.SetRowSpan(_descriptionBox, 1);
        layout.Controls.Add(_enabledBox, 1, 2);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        var ok = new Button { Text = "Save", DialogResult = DialogResult.OK, AutoSize = true };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        layout.Controls.Add(buttons, 0, 3);
        layout.SetColumnSpan(buttons, 2);
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        AcceptButton = ok;
        CancelButton = cancel;
        Controls.Add(layout);
    }

    public void ApplyTo(Workflow workflow)
    {
        workflow.Name = _nameBox.Text.Trim();
        workflow.Description = _descriptionBox.Text.Trim();
        workflow.IsEnabled = _enabledBox.Checked;
    }
}
