using System.IO;
using System.Linq;

namespace WindowsWorkflowAutomator.GitHub;

public static class SmartCommitMessageGenerator
{
    // Simple rule-based commit message generator based on changed file paths
    public static string GenerateMessage(IEnumerable<string> changedFiles)
    {
        if (changedFiles is null)
            return "Update";

        var list = changedFiles.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        if (list.Count == 0)
            return "Update";

        // If all files are under a single top-level folder, use that folder name
        var topLevel = list
            .Select(p => GetTopLevelFolder(p))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (topLevel.Count == 1)
        {
            return $"Update {topLevel[0]}";
        }

        // If many files but they all share a common parent like Models, use that
        var common = list
            .Select(p => Path.GetDirectoryName(p) ?? string.Empty)
            .Select(d => d.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).LastOrDefault() ?? string.Empty)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        if (common is not null && common.Count() >= 2)
        {
            return $"Update {common.Key}";
        }

        if (list.Count == 1)
        {
            // Use filename without extension and Pascal-case it
            var name = Path.GetFileNameWithoutExtension(list[0]);
            if (!string.IsNullOrWhiteSpace(name))
            {
                var shortMsg = name.Replace('-', ' ').Replace('_', ' ');
                return $"Update {shortMsg}";
            }
        }

        // Fallback
        return "Update";
    }

    private static string GetTopLevelFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        var parts = path.Replace('/', Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return string.Empty;

        // If file is at root, return file name without extension
        if (parts.Length == 1)
            return Path.GetFileNameWithoutExtension(parts[0]);

        return parts[0];
    }
}