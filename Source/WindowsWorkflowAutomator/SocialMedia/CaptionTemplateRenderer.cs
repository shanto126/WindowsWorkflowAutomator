namespace WindowsWorkflowAutomator.SocialMedia;

public static class CaptionTemplateRenderer
{
    public static string Render(string? template, IReadOnlyDictionary<string, string> variables)
    {
        var result = template ?? string.Empty;
        foreach (var pair in variables)
        {
            result = result.Replace($"{{{pair.Key}}}", pair.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }
}
