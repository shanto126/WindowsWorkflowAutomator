namespace WindowsWorkflowAutomator.FileOrganizer;

public interface IDownloadFolderMonitor : IDisposable
{
    bool IsRunning { get; }

    string? WatchedFolder { get; }

    void Start(string folderPath);

    void Stop();
}
