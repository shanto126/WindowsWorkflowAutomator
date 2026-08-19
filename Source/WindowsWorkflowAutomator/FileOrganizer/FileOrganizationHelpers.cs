using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.FileOrganizer;

public static class FileOrganizationHelpers
{
    private static readonly HashSet<string> IncompleteExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".tmp",
        ".temp",
        ".crdownload",
        ".part",
        ".partial",
        ".download",
        ".opdownload"
    };

    public static bool IsIncompleteFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return true;
        }

        if (fileName.StartsWith("~$", StringComparison.Ordinal) ||
            fileName.StartsWith(".", StringComparison.Ordinal) && fileName.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var extension = Path.GetExtension(fileName);
        return IncompleteExtensions.Contains(extension);
    }

    public static string NormalizeExtension(string extension)
    {
        var value = extension.Trim();
        if (value.Length == 0)
        {
            return string.Empty;
        }

        return value.StartsWith('.') ? value.ToLowerInvariant() : "." + value.ToLowerInvariant();
    }

    public static IReadOnlyList<string> ParseExtensions(string extensionField)
    {
        if (string.IsNullOrWhiteSpace(extensionField))
        {
            return [];
        }

        return extensionField
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeExtension)
            .Where(x => x.Length > 1)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static FileOrganizationRule? FindMatchingRule(
        IEnumerable<FileOrganizationRule> rules,
        string filePath)
    {
        var extension = NormalizeExtension(Path.GetExtension(filePath));
        if (extension.Length == 0)
        {
            return null;
        }

        foreach (var rule in rules.Where(r => r.IsEnabled).OrderBy(r => r.Id))
        {
            if (ParseExtensions(rule.Extension).Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return rule;
            }
        }

        return null;
    }

    public static string BuildTargetFileName(string sourceFilePath, FileOrganizationRule rule)
    {
        var originalName = Path.GetFileNameWithoutExtension(sourceFilePath);
        var extWithoutDot = Path.GetExtension(sourceFilePath).TrimStart('.');
        var now = DateTime.Now;

        if (string.IsNullOrWhiteSpace(rule.RenamePattern) && rule.Action != FileOrganizationAction.Rename)
        {
            return Path.GetFileName(sourceFilePath);
        }

        var pattern = string.IsNullOrWhiteSpace(rule.RenamePattern)
            ? "{name}_{yyyyMMdd}{ext}"
            : rule.RenamePattern;

        var fileName = pattern
            .Replace("{name}", originalName, StringComparison.OrdinalIgnoreCase)
            .Replace("{yyyyMMdd}", now.ToString("yyyyMMdd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{HHmmss}", now.ToString("HHmmss"), StringComparison.OrdinalIgnoreCase);

        if (fileName.Contains("{ext}", StringComparison.OrdinalIgnoreCase))
        {
            fileName = fileName.Replace("{ext}", extWithoutDot.Length == 0 ? string.Empty : "." + extWithoutDot, StringComparison.OrdinalIgnoreCase);
        }
        else if (string.IsNullOrEmpty(Path.GetExtension(fileName)) && extWithoutDot.Length > 0)
        {
            fileName += "." + extWithoutDot;
        }

        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalid, '_');
        }

        return fileName;
    }

    public static string GetAvailablePath(string destinationDirectory, string fileName)
    {
        var destination = Path.Combine(destinationDirectory, fileName);
        if (!File.Exists(destination) && !Directory.Exists(destination))
        {
            return destination;
        }

        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var index = 1;
        while (true)
        {
            var candidate = Path.Combine(destinationDirectory, $"{name} ({index}){extension}");
            if (!File.Exists(candidate) && !Directory.Exists(candidate))
            {
                return candidate;
            }

            index++;
        }
    }
}
