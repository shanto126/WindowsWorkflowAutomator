using System.Text.Json;
using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Configuration;

public sealed class AppSettingsService : IAppSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly AppPaths _paths;
    private readonly IAppLogger _logger;

    public AppSettingsService(AppPaths paths, IAppLogger logger)
    {
        _paths = paths;
        _logger = logger;
        Current = new AppSettings();
        Reload();
    }

    public AppSettings Current { get; private set; }

    public void Reload()
    {
        try
        {
            if (!File.Exists(_paths.UserSettingsFilePath))
            {
                Current = new AppSettings();
                Save();
                return;
            }

            var json = File.ReadAllText(_paths.UserSettingsFilePath);
            Current = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            _logger.Warning("Could not load settings. Using defaults.");
            _logger.Error("Settings load failed.", ex);
            Current = new AppSettings();
        }
    }

    public void Save()
    {
        _paths.EnsureCreated();
        var json = JsonSerializer.Serialize(Current, JsonOptions);
        File.WriteAllText(_paths.UserSettingsFilePath, json);
        _logger.Information("Settings saved.");
    }
}
