using WindowsWorkflowAutomator.FileOrganizer;
using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Tests;

public class FileOrganizerTests
{
    [Fact]
    public void ParseExtensions_NormalizesAndSplits()
    {
        var parsed = FileOrganizationHelpers.ParseExtensions("PDF, .Jpg; png");

        Assert.Equal([".pdf", ".jpg", ".png"], parsed);
    }

    [Fact]
    public void FindMatchingRule_UsesEnabledExtension()
    {
        var rules = new[]
        {
            new FileOrganizationRule { Id = 1, Extension = "txt", IsEnabled = false, DestinationFolder = @"C:\a" },
            new FileOrganizationRule { Id = 2, Extension = ".pdf,.PDF", IsEnabled = true, DestinationFolder = @"C:\docs" }
        };

        var match = FileOrganizationHelpers.FindMatchingRule(rules, @"C:\inbox\report.PDF");

        Assert.NotNull(match);
        Assert.Equal(2, match!.Id);
        Assert.Null(FileOrganizationHelpers.FindMatchingRule(rules, @"C:\inbox\notes.txt"));
        Assert.Null(FileOrganizationHelpers.FindMatchingRule(rules, @"C:\inbox\photo.jpg"));
    }

    [Fact]
    public void GetAvailablePath_AppendsNumberWhenDuplicate()
    {
        var directory = Directory.CreateTempSubdirectory("wwa-fileorg-");
        try
        {
            var original = Path.Combine(directory.FullName, "file.txt");
            File.WriteAllText(original, "a");

            var available = FileOrganizationHelpers.GetAvailablePath(directory.FullName, "file.txt");

            Assert.Equal(Path.Combine(directory.FullName, "file (1).txt"), available);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Fact]
    public async Task OrganizeFileAsync_MovesMatchingFile()
    {
        var watch = Directory.CreateTempSubdirectory("wwa-watch-");
        var dest = Directory.CreateTempSubdirectory("wwa-dest-");
        try
        {
            var source = Path.Combine(watch.FullName, "notes.txt");
            File.WriteAllText(source, "hello");

            var rules = new StubRuleService();
            rules.Items.Add(new FileOrganizationRule
            {
                Id = 1,
                Extension = "txt",
                DestinationFolder = dest.FullName,
                Action = FileOrganizationAction.Move,
                IsEnabled = true
            });

            var service = new FileOrganizerService(rules, new StubLogger());
            var result = await service.OrganizeFileAsync(source);

            Assert.True(result.Succeeded, result.Message);
            Assert.False(File.Exists(source));
            Assert.True(File.Exists(Path.Combine(dest.FullName, "notes.txt")));
        }
        finally
        {
            watch.Delete(true);
            dest.Delete(true);
        }
    }

    [Fact]
    public async Task DownloadFolderMonitor_MovesFileAfterCreate()
    {
        var watch = Directory.CreateTempSubdirectory("wwa-mon-w-");
        var dest = Directory.CreateTempSubdirectory("wwa-mon-d-");
        try
        {
            var rules = new StubRuleService();
            rules.Items.Add(new FileOrganizationRule
            {
                Id = 1,
                Extension = "png",
                DestinationFolder = dest.FullName,
                Action = FileOrganizationAction.Move,
                IsEnabled = true
            });

            var organizer = new FileOrganizerService(rules, new StubLogger());
            using var monitor = new DownloadFolderMonitor(organizer, new StubLogger());
            monitor.Start(watch.FullName);

            var source = Path.Combine(watch.FullName, "shot.png");
            var destination = Path.Combine(dest.FullName, "shot.png");
            await File.WriteAllTextAsync(source, "image-bytes");

            var deadline = DateTime.UtcNow.AddSeconds(12);
            while (DateTime.UtcNow < deadline && !File.Exists(destination))
            {
                await Task.Delay(200);
            }

            Assert.True(File.Exists(destination), "Watched file should be moved to the destination folder.");
            Assert.False(File.Exists(source));
        }
        finally
        {
            watch.Delete(true);
            dest.Delete(true);
        }
    }

    private sealed class StubRuleService : IFileRuleService
    {
        public List<FileOrganizationRule> Items { get; } = [];

        public Task<IReadOnlyList<FileOrganizationRule>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FileOrganizationRule>>(Items);

        public Task<FileOrganizationRule> AddAsync(FileOrganizationRule rule, CancellationToken cancellationToken = default)
        {
            Items.Add(rule);
            return Task.FromResult(rule);
        }

        public Task UpdateAsync(FileOrganizationRule rule, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(int id, CancellationToken cancellationToken = default) => Task.CompletedTask;
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
}
