using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.FileOrganizer;

public interface IFileOrganizerService
{
    IReadOnlyList<FileOrganizationActionLog> RecentActions { get; }

    event EventHandler<FileOrganizationActionLog>? ActionRecorded;

    Task<IReadOnlyList<FileOrganizationActionLog>> OrganizeFolderAsync(
        string folderPath,
        CancellationToken cancellationToken = default);

    Task<FileOrganizationActionLog> OrganizeFileAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}
