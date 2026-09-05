using WindowsWorkflowAutomator.UI.Theme;

namespace WindowsWorkflowAutomator.UI;

public static class ButtonStyleExtensions
{
    public static void ApplyPrimaryStyle(this Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = AppTheme.AccentBlue;
        button.ForeColor = AppTheme.CardBackground;
        button.Cursor = Cursors.Hand;
        button.MouseEnter -= OnPrimaryMouseEnter;
        button.MouseEnter += OnPrimaryMouseEnter;
        button.MouseLeave -= OnPrimaryMouseLeave;
        button.MouseLeave += OnPrimaryMouseLeave;
    }

    public static void ApplySecondaryStyle(this Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = AppTheme.BorderColor;
        button.BackColor = AppTheme.CardBackground;
        button.ForeColor = AppTheme.TextPrimary;
        button.Cursor = Cursors.Hand;
    }

    private static void OnPrimaryMouseEnter(object? sender, EventArgs e)
    {
        if (sender is Button button)
        {
            button.BackColor = ControlPaint.Dark(AppTheme.AccentBlue, 0.1f);
        }
    }

    private static void OnPrimaryMouseLeave(object? sender, EventArgs e)
    {
        if (sender is Button button)
        {
            button.BackColor = AppTheme.AccentBlue;
        }
    }
}
