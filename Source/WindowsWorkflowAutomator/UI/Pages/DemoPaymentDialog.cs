using WindowsWorkflowAutomator.UI;
using WindowsWorkflowAutomator.UI.Theme;

namespace WindowsWorkflowAutomator.UI.Pages;

public sealed class DemoPaymentDialog : Form
{
    private readonly ComboBox _paymentMethod = new();

    public string PaymentMethod => _paymentMethod.SelectedItem?.ToString() ?? "Demo bKash";

    public DemoPaymentDialog()
    {
        Text = "Demo Payment";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(360, 190);
        Padding = new Padding(16);

        var title = new Label
        {
            Text = "Complete demo payment",
            AutoSize = true,
            Font = AppTheme.Heading,
            Dock = DockStyle.Top
        };
        var description = new Label
        {
            Text = "No real payment will be processed.",
            AutoSize = true,
            Font = AppTheme.Body,
            ForeColor = AppTheme.TextSecondary,
            Padding = new Padding(0, 6, 0, 14),
            Dock = DockStyle.Top
        };
        var methodLabel = new Label
        {
            Text = "Payment method",
            AutoSize = true,
            Dock = DockStyle.Top
        };

        _paymentMethod.DropDownStyle = ComboBoxStyle.DropDownList;
        _paymentMethod.Items.AddRange(["Demo bKash", "Demo Card"]);
        _paymentMethod.SelectedIndex = 0;
        _paymentMethod.Dock = DockStyle.Top;
        _paymentMethod.Margin = new Padding(0, 4, 0, 12);

        var payButton = new Button
        {
            Text = "Pay Now",
            DialogResult = DialogResult.OK,
            AutoSize = true,
            Dock = DockStyle.Right
        };
        payButton.ApplyPrimaryStyle();

        var cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            AutoSize = true,
            Dock = DockStyle.Right
        };
        cancelButton.ApplySecondaryStyle();

        var actions = new Panel { Dock = DockStyle.Bottom, Height = 38 };
        actions.Controls.Add(cancelButton);
        actions.Controls.Add(payButton);

        Controls.Add(actions);
        Controls.Add(_paymentMethod);
        Controls.Add(methodLabel);
        Controls.Add(description);
        Controls.Add(title);
        AcceptButton = payButton;
        CancelButton = cancelButton;
    }
}
