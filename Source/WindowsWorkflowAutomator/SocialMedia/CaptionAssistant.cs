using System.Text.RegularExpressions;

namespace WindowsWorkflowAutomator.SocialMedia;

/// <summary>
/// Produces a polished, local caption from a short user brief. This is deliberately
/// deterministic so drafting captions does not require an API key or network access.
/// </summary>
public static partial class CaptionAssistant
{
    public static string Generate(string? brief, IReadOnlyDictionary<string, string>? variables = null)
    {
        var subject = NormalizeBrief(brief);
        var platform = GetVariable(variables, "Platform");
        var date = GetVariable(variables, "Date");

        var caption = $"{subject}\n\nA small step forward, made to be shared. What do you think?";
        if (!string.IsNullOrWhiteSpace(platform))
        {
            caption += $"\n\n#{ToHashtag(platform)}";
        }

        if (!string.IsNullOrWhiteSpace(date))
        {
            caption += $" #{ToHashtag(date)}";
        }

        return caption;
    }

    private static string NormalizeBrief(string? brief)
    {
        var normalized = Whitespace().Replace(brief?.Trim() ?? string.Empty, " ");
        return string.IsNullOrWhiteSpace(normalized) ? "Something new is on the way." : normalized;
    }

    private static string GetVariable(IReadOnlyDictionary<string, string>? variables, string key) =>
        variables is not null && variables.TryGetValue(key, out var value) ? value : string.Empty;

    private static string ToHashtag(string value) =>
        NonWord().Replace(value, string.Empty);

    [GeneratedRegex("\\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex("[^A-Za-z0-9]")]
    private static partial Regex NonWord();
}
