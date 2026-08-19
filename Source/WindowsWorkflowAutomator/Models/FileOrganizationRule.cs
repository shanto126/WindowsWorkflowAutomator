namespace WindowsWorkflowAutomator.Models;

public sealed class FileOrganizationRule
{
    public int Id { get; set; }

    /// <summary>
    /// File extension(s) without or with a leading dot. Comma-separated values are allowed (e.g. "pdf" or ".jpg,.png").
    /// </summary>
    public string Extension { get; set; } = string.Empty;

    public string DestinationFolder { get; set; } = string.Empty;

    public FileOrganizationAction Action { get; set; } = FileOrganizationAction.Move;

    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Optional name template. Tokens: {name}, {ext}, {yyyyMMdd}, {HHmmss}.
    /// </summary>
    public string? RenamePattern { get; set; }
}
