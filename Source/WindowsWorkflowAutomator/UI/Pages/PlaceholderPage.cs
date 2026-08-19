namespace WindowsWorkflowAutomator.UI.Pages;

public class PlaceholderPage : UserControl
{
    public PlaceholderPage()
        : this("Module", "This module has not been implemented yet.")
    {
    }

    public PlaceholderPage(string title, string description)
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(246, 248, 252);
        Padding = new Padding(32);

        var titleLabel = new Label
        {
            Text = title,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
            ForeColor = Color.FromArgb(17, 24, 39)
        };

        var descriptionLabel = new Label
        {
            Text = description,
            AutoSize = true,
            MaximumSize = new Size(720, 0),
            Font = new Font("Segoe UI", 11F),
            ForeColor = Color.FromArgb(75, 85, 99),
            Padding = new Padding(0, 12, 0, 0)
        };

        var noteLabel = new Label
        {
            Text = "Feature implementation is intentionally deferred to a later phase.",
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Italic),
            ForeColor = Color.FromArgb(107, 114, 128),
            Padding = new Padding(0, 24, 0, 0)
        };

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true
        };

        layout.Controls.Add(titleLabel);
        layout.Controls.Add(descriptionLabel);
        layout.Controls.Add(noteLabel);
        Controls.Add(layout);
    }
}
