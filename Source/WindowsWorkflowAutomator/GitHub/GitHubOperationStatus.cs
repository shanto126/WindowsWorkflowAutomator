namespace WindowsWorkflowAutomator.GitHub;

public enum GitHubOperationStatus
{
    Success,
    NoChanges,
    NotConfigured,
    InvalidRepository,
    InvalidBranch,
    AuthFailure,
    MergeConflict,
    NetworkFailure,
    GitNotInstalled,
    InvalidRemote,
    Error
}
