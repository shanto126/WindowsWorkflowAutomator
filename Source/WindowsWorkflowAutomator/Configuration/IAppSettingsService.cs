using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Configuration;

public interface IAppSettingsService
{
    AppSettings Current { get; }
    void Save();
    void Reload();
}
