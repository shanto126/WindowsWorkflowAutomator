using WindowsWorkflowAutomator.UI.Theme;

namespace WindowsWorkflowAutomator.UI;

public static class GridStyleExtensions
{
    public static void ApplyModernStyle(this DataGridView grid)
    {
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = AppTheme.AccentBlue,
            ForeColor = AppTheme.CardBackground,
            Font = AppTheme.Heading,
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };
        grid.ColumnHeadersHeight = 32;
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = AppTheme.CardBackground,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.Body,
            SelectionBackColor = ControlPaint.Light(AppTheme.AccentBlue),
            SelectionForeColor = AppTheme.TextPrimary
        };
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = AppTheme.Background,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.Body,
            SelectionBackColor = ControlPaint.Light(AppTheme.AccentBlue),
            SelectionForeColor = AppTheme.TextPrimary
        };
        grid.RowTemplate.Height = 32;
        grid.GridColor = AppTheme.BorderColor;
    }
}
