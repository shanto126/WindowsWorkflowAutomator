using Microsoft.Extensions.DependencyInjection;
using WindowsWorkflowAutomator.Configuration;
using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Models;
using WindowsWorkflowAutomator.Security;
using WindowsWorkflowAutomator.SocialMedia;
using Xunit;

namespace WindowsWorkflowAutomator.Tests;

public class TikTokServiceTests
{
    [Fact]
    public async Task ValidateConnection_ReturnsNotConfigured_WhenNoToken()
    {
        var settings = new InMemorySettingsService();
        var protector = new WindowsSecretProtector();
        var logger = new NoopLogger();
        var service = new TikTokService(settings, protector, logger);

        var result = await service.ValidateConnectionAsync();
        Assert.False(result.Succeeded);
        Assert.Equal(PlatformOperationStatus.NotConfigured, result.Status);
    }

    [Fact]
    public async Task UploadMedia_ReturnsLimited_ForLocalFile()
    {
        var settings = new InMemorySettingsService();
        var protector = new WindowsSecretProtector();
        var logger = new NoopLogger();
        var service = new TikTokService(settings, protector, logger);

        var result = await service.UploadMediaAsync("C:\\nonexistent\\video.mp4");
        Assert.False(result.Succeeded);
        Assert.Equal(PlatformOperationStatus.Limited, result.Status);
    }
}

// Test helpers (simple in-memory settings and no-op logger)
internal sealed class InMemorySettingsService : IAppSettingsService
{
    public AppSettings Current { get; private set; } = new();

    public void Save() { }
    public void Reload() { }
}

internal sealed class NoopLogger : IAppLogger
{
    public void Information(string message) { }
    public void Warning(string message) { }
    public void Error(string message, Exception? exception = null) { }
}