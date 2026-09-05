using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WindowsWorkflowAutomator.Configuration;
using WindowsWorkflowAutomator.Data;
using WindowsWorkflowAutomator.GitHub;
using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Models;
using WindowsWorkflowAutomator.Repositories;
using WindowsWorkflowAutomator.Security;

namespace WindowsWorkflowAutomator.Tests;


public sealed class GitHubAutomationTests
{
    [Fact]
    public async Task Configure_And_Commit_LocalRepo_Works_WithoutPush()
    {
        var root = Path.Combine(Path.GetTempPath(), "wwa-gh-tests", Guid.NewGuid().ToString("N"));
        var repoPath = Path.Combine(root, "repo");
        var dbPath = Path.Combine(root, "app.db");
        Directory.CreateDirectory(repoPath);
        ServiceProvider? provider = null;

        try
        {
            RunGit(repoPath, "init");
            RunGit(repoPath, "config", "user.email", "test@example.com");
            RunGit(repoPath, "config", "user.name", "WWA Test");

            File.WriteAllText(Path.Combine(repoPath, "README.md"), "initial");
            RunGit(repoPath, "add", "README.md");
            RunGit(repoPath, "commit", "-m", "chore: initial");

            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));
            services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
            services.AddSingleton<IAppSettingsService>(new InMemorySettingsService());
            services.AddSingleton<ISecretProtector, WindowsSecretProtector>();
            services.AddSingleton<IAppLogger, NoopLogger>();
            services.AddSingleton<IGitHubService, GitHubService>();

            provider = services.BuildServiceProvider();
            using (var scope = provider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.EnsureSchema();
            }

            var gitHub = provider.GetRequiredService<IGitHubService>();

            var configured = await gitHub.ConfigureRepositoryAsync(
                repoPath,
                "https://github.com/example/repo.git",
                "chore: backup changes",
                "dummy-token");

            Assert.True(configured.Succeeded, configured.Message);

            File.AppendAllText(Path.Combine(repoPath, "README.md"), Environment.NewLine + "change");
            var committed = await gitHub.CommitAsync("test: local commit");

            Assert.True(committed.Succeeded, committed.Message);

            var log = RunGit(repoPath, "log", "-1", "--pretty=%s");
            Assert.Equal("test: local commit", log.Trim());
        }
        finally
        {
            provider?.Dispose();
            if (Directory.Exists(root))
            {
                try
                {
                    Directory.Delete(root, recursive: true);
                }
                catch (IOException)
                {
                    // SQLite may keep a short-lived file lock; test assertions are already complete.
                }
            }
        }
    }

    private static string RunGit(string workingDirectory, params string[] args)
    {
        var info = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in args)
        {
            info.ArgumentList.Add(arg);
        }

        using var process = Process.Start(info) ?? throw new InvalidOperationException("Could not start git process.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"git command failed: {string.Join(' ', args)}\n{error}");
        }

        return output;
    }

    private sealed class InMemorySettingsService : IAppSettingsService
    {
        public AppSettings Current { get; private set; } = new();

        public void Save()
        {
        }

        public void Reload()
        {
        }
    }

    private sealed class NoopLogger : IAppLogger
    {
        public void Information(string message)
        {
        }

        public void Warning(string message)
        {
        }

        public void Error(string message, Exception? exception = null)
        {
        }
    }
}
