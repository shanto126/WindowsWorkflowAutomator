using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.UI.Pages;

internal sealed class FileOrganizerRuleDialog : Form
{
    private readonly TextBox _extensionBox = new();
    private readonly TextBox _destinationBox = new();
    private readonly ComboBox _actionBox = new();
    private readonly TextBox _patternBox = new();
    private readonly CheckBox _enabledBox = new();

    public FileOrganizerRuleDialog(FileOrganizationRule? existing)
    {
        Text = existing is null ? "Add rule" : "Edit rule";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(520, 280);
        Font = new Font("Segoe UI", 9.5F);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 6,
            Padding = new Padding(16)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));

        AddRow(layout, 0, "Extension(s)", _extensionBox, null);
        AddRow(layout, 1, "Destination", _destinationBox, CreateBrowseButton());
        AddRow(layout, 2, "Action", _actionBox, null);
        AddRow(layout, 3, "Rename pattern", _patternBox, null);

        _actionBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _actionBox.Items.AddRange(Enum.GetNames<FileOrganizationAction>());
        _actionBox.SelectedItem = (existing?.Action ?? FileOrganizationAction.Move).ToString();

        _extensionBox.Text = existing?.Extension ?? string.Empty;
        _destinationBox.Text = existing?.DestinationFolder ?? string.Empty;
        _patternBox.Text = existing?.RenamePattern ?? string.Empty;
        _extensionBox.PlaceholderText = "pdf  or  .jpg,.png";
        _patternBox.PlaceholderText = "Optional: {name}_{yyyyMMdd}{ext}";

        _enabledBox.Text = "Enabled";
        _enabledBox.Checked = existing?.IsEnabled ?? true;
        _enabledBox.AutoSize = true;
        layout.Controls.Add(_enabledBox, 1, 4);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        var ok = new Button { Text = "Save", DialogResult = DialogResult.OK, AutoSize = true };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        layout.Controls.Add(buttons, 0, 5);
        layout.SetColumnSpan(buttons, 3);

        AcceptButton = ok;
        CancelButton = cancel;
        Controls.Add(layout);
    }

    public FileOrganizationRule ToRule(int id)
    {
        var action = Enum.Parse<FileOrganizationAction>(_actionBox.SelectedItem?.ToString() ?? nameof(FileOrganizationAction.Move));
        return new FileOrganizationRule
        {
            Id = id,
            Extension = _extensionBox.Text.Trim(),
            DestinationFolder = _destinationBox.Text.Trim(),
            Action = action,
            IsEnabled = _enabledBox.Checked,
            RenamePattern = string.IsNullOrWhiteSpace(_patternBox.Text) ? null : _patternBox.Text.Trim()
        };
    }

    private Button CreateBrowseButton()
    {
        var button = new Button { Text = "Browse", Dock = DockStyle.Fill };
        button.Click += (_, _) =>
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Choose the destination folder",
                UseDescriptionForTitle = true,
                SelectedPath = _destinationBox.Text
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _destinationBox.Text = dialog.SelectedPath;
            }
        };
        return button;
    }

    private static void AddRow(TableLayoutPanel layout, int row, string label, Control field, Control? extra)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        var caption = new Label
        {
            Text = label,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            TextAlign = ContentAlignment.MiddleLeft
        };
        field.Dock = DockStyle.Fill;
        layout.Controls.Add(caption, 0, row);
        layout.Controls.Add(field, 1, row);
        if (extra is not null)
        {
            layout.Controls.Add(extra, 2, row);
        }
        else
        {
            layout.SetColumnSpan(field, 2);
        }
    }
}
