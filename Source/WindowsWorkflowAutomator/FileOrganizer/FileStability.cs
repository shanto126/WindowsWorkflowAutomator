namespace WindowsWorkflowAutomator.FileOrganizer;

internal static class FileStability
{
    public static async Task<bool> WaitUntilReadyAsync(
        string filePath,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;
        long lastSize = -1;
        var stableHits = 0;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!File.Exists(filePath))
            {
                return false;
            }

            if (FileOrganizationHelpers.IsIncompleteFileName(Path.GetFileName(filePath)))
            {
                await Task.Delay(400, cancellationToken);
                continue;
            }

            try
            {
                var info = new FileInfo(filePath);
                info.Refresh();
                using (new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    if (info.Length == lastSize)
                    {
                        stableHits++;
                    }
                    else
                    {
                        lastSize = info.Length;
                        stableHits = 1;
                    }
                }

                if (stableHits >= 2)
                {
                    return true;
                }
            }
            catch (IOException)
            {
                stableHits = 0;
            }
            catch (UnauthorizedAccessException)
            {
                stableHits = 0;
            }

            await Task.Delay(400, cancellationToken);
        }

        return false;
    }
}
