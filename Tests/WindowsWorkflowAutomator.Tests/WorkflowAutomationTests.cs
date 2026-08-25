using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WindowsWorkflowAutomator.Automation;
using WindowsWorkflowAutomator.Data;
using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Models;
using WindowsWorkflowAutomator.Services.Automation;
using WindowsWorkflowAutomator.Licensing;

namespace WindowsWorkflowAutomator.Tests;

public class WorkflowAutomationTests
{
    [Fact]
    public async Task OpenApplicationAction_FailsWhenPathMissing()
    {
        var action = new OpenApplicationAction(new RecordingLauncher());
        var error = await Assert.ThrowsAsync<WorkflowActionException>(() =>
            action.ExecuteAsync(new WorkflowAction
            {
                Type = WorkflowActionType.OpenApplication,
                Target = @"C:\this-app-does-not-exist-wwa.exe"
            }));

        Assert.Contains("not found", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OpenFolderAction_FailsWhenFolderMissing()
    {
        var action = new OpenFolderAction(new RecordingLauncher());
        var error = await Assert.ThrowsAsync<WorkflowActionException>(() =>
            action.ExecuteAsync(new WorkflowAction
            {
                Type = WorkflowActionType.OpenFolder,
                Target = @"C:\wwa-missing-folder-test"
            }));

        Assert.Contains("missing", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OpenWebsiteAction_FailsWhenUrlInvalid()
    {
        var action = new OpenWebsiteAction(new RecordingLauncher(), new FakeReachability());
        var error = await Assert.ThrowsAsync<WorkflowActionException>(() =>
            action.ExecuteAsync(new WorkflowAction
            {
                Type = WorkflowActionType.OpenWebsite,
                Target = "not a url"
            }));

        Assert.Contains("invalid", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WorkflowService_RunsActionsInOrder()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"wwa-wf-{Guid.NewGuid():N}.db");
        var launcher = new RecordingLauncher();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));
        await using var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<AppDbContext>().EnsureSchema();
        }

        var factory = new WorkflowActionFactory(
        [
            new OpenApplicationAction(launcher),
            new OpenWebsiteAction(launcher, new FakeReachability()),
            new OpenFolderAction(launcher)
        ]);
        var service = new WorkflowService(provider.GetRequiredService<IServiceScopeFactory>(), factory, new StubLogger(), new StubLicenseService());


        var folder = Directory.CreateTempSubdirectory("wwa-wf-folder-");
        var exe = Path.Combine(folder.FullName, "fake.exe");
        await File.WriteAllBytesAsync(exe, [0]);
        try
        {
            var created = await service.CreateAsync(new Workflow
            {
                Name = "Morning",
                Description = "Test",
                IsEnabled = true,
                Actions =
                [
                    new() { Type = WorkflowActionType.OpenApplication, Target = exe },
                    new() { Type = WorkflowActionType.OpenWebsite, Target = "https://example.com" },
                    new() { Type = WorkflowActionType.OpenFolder, Target = folder.FullName }
                ]
            });

            var result = await service.RunAsync(created.Id);

            Assert.True(result.Succeeded, string.Join(" | ", result.Steps.Select(s => s.Message)));
            Assert.Equal(3, launcher.Starts.Count);
            Assert.Equal(exe, launcher.Starts[0].FileName);
            Assert.Equal("https://example.com/", launcher.Starts[1].FileName);
            Assert.Equal(folder.FullName, launcher.Starts[2].FileName);
        }
        finally
        {
            folder.Delete(true);
            await provider.DisposeAsync();
            SqliteConnection.ClearAllPools();
            File.Delete(dbPath);
        }
    }

    private sealed class RecordingLauncher : IProcessLauncher
    {
        public List<(string FileName, string? Arguments)> Starts { get; } = [];

        public void Start(string fileName, string? arguments, bool useShellExecute) =>
            Starts.Add((fileName, arguments));
    }

    private sealed class FakeReachability : IUrlReachabilityService
    {
        public Task EnsureReachableAsync(Uri uri, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubLogger : IAppLogger
    {
        public void Error(string message, Exception? exception = null)
        {
        }

        public void Information(string message)
        {
        }

        public void Warning(string message)
        {
        }
    }

    private sealed class StubLicenseService : ILicenseService
    {
        public bool IsPremium => false;
        public string CurrentTier => "Free";
        public Task<bool> ActivateAsync(string licenseKey, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> ValidateAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task DeactivateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
