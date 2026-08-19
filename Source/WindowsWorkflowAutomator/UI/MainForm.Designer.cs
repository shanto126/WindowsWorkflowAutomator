#nullable enable

namespace WindowsWorkflowAutomator.UI;

partial class MainForm
{
    private System.ComponentModel.IContainer components = new System.ComponentModel.Container();
    private Panel sidebarPanel = null!;
    private Panel headerPanel = null!;
    private Panel contentPanel = null!;
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel statusLabel = null!;
    private Label sidebarTitleLabel = null!;
    private Label headerTitleLabel = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        sidebarPanel = new Panel();
        sidebarTitleLabel = new Label();
        headerPanel = new Panel();
        headerTitleLabel = new Label();
        contentPanel = new Panel();
        statusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel();
        sidebarPanel.SuspendLayout();
        headerPanel.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();

        sidebarPanel.BackColor = Color.FromArgb(17, 24, 39);
        sidebarPanel.Dock = DockStyle.Left;
        sidebarPanel.Width = 240;
        sidebarPanel.Padding = new Padding(0, 56, 0, 0);
        sidebarPanel.Controls.Add(sidebarTitleLabel);

        sidebarTitleLabel.Text = "WWA";
        sidebarTitleLabel.ForeColor = Color.White;
        sidebarTitleLabel.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
        sidebarTitleLabel.AutoSize = true;
        sidebarTitleLabel.Location = new Point(16, 18);

        headerPanel.BackColor = Color.White;
        headerPanel.Dock = DockStyle.Top;
        headerPanel.Height = 56;
        headerPanel.Padding = new Padding(20, 0, 20, 0);
        headerPanel.Controls.Add(headerTitleLabel);

        headerTitleLabel.Text = "Dashboard";
        headerTitleLabel.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
        headerTitleLabel.ForeColor = Color.FromArgb(17, 24, 39);
        headerTitleLabel.Dock = DockStyle.Fill;
        headerTitleLabel.TextAlign = ContentAlignment.MiddleLeft;

        contentPanel.Dock = DockStyle.Fill;
        contentPanel.BackColor = Color.FromArgb(246, 248, 252);
        contentPanel.Padding = new Padding(0);

        statusLabel.Text = "Ready";
        statusStrip.Items.Add(statusLabel);
        statusStrip.SizingGrip = false;

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1180, 720);
        MinimumSize = new Size(960, 600);
        Text = "Windows Workflow Automator";
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);
        BackColor = Color.FromArgb(246, 248, 252);
        Controls.Add(contentPanel);
        Controls.Add(headerPanel);
        Controls.Add(statusStrip);
        Controls.Add(sidebarPanel);
        sidebarPanel.ResumeLayout(false);
        headerPanel.ResumeLayout(false);
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
