namespace WindowsWorkflowAutomator.SocialMedia;

public static class SocialPostPlanner
{
    private static readonly HashSet<string> SupportedExtensions =
    [
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp"
    ];

    public static IReadOnlyList<IReadOnlyList<string>> SplitImagePaths(
        IReadOnlyList<string> imagePaths,
        int postCount,
        int imagesPerPost)
    {
        if (postCount <= 0)
        {
            throw new ArgumentException("Post count must be greater than zero.", nameof(postCount));
        }

        if (imagesPerPost <= 0)
        {
            throw new ArgumentException("Images per post must be greater than zero.", nameof(imagesPerPost));
        }

        var filtered = imagePaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Where(path => SupportedExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .ToList();

        var required = postCount * imagesPerPost;
        if (filtered.Count < required)
        {
            throw new InvalidOperationException(
                $"Not enough images. Required {required}, but found {filtered.Count} supported image(s).");
        }

        var groups = new List<IReadOnlyList<string>>(postCount);
        for (var i = 0; i < postCount; i++)
        {
            var start = i * imagesPerPost;
            groups.Add(filtered.Skip(start).Take(imagesPerPost).ToArray());
        }

        return groups;
    }

    public static IReadOnlyList<string> GetSupportedImagesFromFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException("Image folder does not exist.");
        }

        return Directory.EnumerateFiles(folderPath, "*", SearchOption.TopDirectoryOnly)
            .Where(path => SupportedExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
            .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
