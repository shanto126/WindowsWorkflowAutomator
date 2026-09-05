using System.Drawing;

namespace WindowsWorkflowAutomator.UI.Theme;

public static class AppTheme
{
    public static readonly Color Background = Color.FromArgb(246, 248, 252);
    public static readonly Color CardBackground = Color.White;
    public static readonly Color TextPrimary = Color.FromArgb(17, 24, 39);
    public static readonly Color TextSecondary = Color.FromArgb(75, 85, 99);
    public static readonly Color AccentBlue = Color.FromArgb(37, 99, 235);
    public static readonly Color AccentGreen = Color.FromArgb(22, 163, 74);
    public static readonly Color BorderColor = Color.FromArgb(229, 231, 235);

    public static Font Title => new("Segoe UI Semibold", 20F, FontStyle.Bold);
    public static Font Heading => new("Segoe UI Semibold", 11F);
    public static Font Body => new("Segoe UI", 9.5F);
}
