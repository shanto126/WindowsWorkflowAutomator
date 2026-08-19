namespace WindowsWorkflowAutomator.GitHub;

public class GitHubOperationResult
{
    public bool Succeeded { get; init; }

    public GitHubOperationStatus Status { get; init; }

    public string Message { get; init; } = string.Empty;

    public IReadOnlyList<string> Details { get; init; } = [];

    public static GitHubOperationResult Ok(string message, IReadOnlyList<string>? details = null) => new()
    {
        Succeeded = true,
        Status = GitHubOperationStatus.Success,
        Message = message,
        Details = details ?? []
    };

    public static GitHubOperationResult Fail(
        GitHubOperationStatus status,
        string message,
        IReadOnlyList<string>? details = null) => new()
    {
        Succeeded = false,
        Status = status,
        Message = message,
        Details = details ?? []
    };
}

public sealed class GitHubStatusResult : GitHubOperationResult
{
    public int ChangedFilesCount { get; init; }

    public IReadOnlyList<string> ChangedFiles { get; init; } = [];

    public static GitHubStatusResult FromFailure(GitHubOperationResult result) => new()
    {
        Succeeded = result.Succeeded,
        Status = result.Status,
        Message = result.Message,
        Details = result.Details,
        ChangedFilesCount = 0,
        ChangedFiles = []
    };
}
