using WindowsWorkflowAutomator.Configuration;

namespace WindowsWorkflowAutomator.Logging;

public sealed class FileAppLogger : IAppLogger
{
    private readonly object _sync = new();
    private readonly AppPaths _paths;

    public FileAppLogger(AppPaths paths)
    {
        _paths = paths;
        _paths.EnsureCreated();
    }

    public void Information(string message) => Write("INF", message, null);

    public void Warning(string message) => Write("WRN", message, null);

    public void Error(string message, Exception? exception = null) => Write("ERR", message, exception);

    private void Write(string level, string message, Exception? exception)
    {
        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {message}";
        if (exception is not null)
        {
            line += Environment.NewLine + exception;
        }

        var filePath = Path.Combine(_paths.LogsDirectory, $"app-{DateTime.Now:yyyyMMdd}.log");
        lock (_sync)
        {
            File.AppendAllText(filePath, line + Environment.NewLine);
        }
    }
}
