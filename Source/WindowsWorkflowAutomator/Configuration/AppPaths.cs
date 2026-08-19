namespace WindowsWorkflowAutomator.Configuration;

public sealed class AppPaths
{
    public const string ApplicationName = "WindowsWorkflowAutomator";

    public AppPaths()
    {
        RootDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ApplicationName);
        LogsDirectory = Path.Combine(RootDirectory, "logs");
        DataDirectory = Path.Combine(RootDirectory, "data");
        DatabaseFilePath = Path.Combine(DataDirectory, "app.db");
        UserSettingsFilePath = Path.Combine(RootDirectory, "settings.json");
    }

    public string RootDirectory { get; }
    public string LogsDirectory { get; }
    public string DataDirectory { get; }
    public string DatabaseFilePath { get; }
    public string UserSettingsFilePath { get; }

    public void EnsureCreated()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(DataDirectory);
    }
}
