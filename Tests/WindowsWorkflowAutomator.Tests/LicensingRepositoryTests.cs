using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WindowsWorkflowAutomator.Data;
using WindowsWorkflowAutomator.Models;
using WindowsWorkflowAutomator.Repositories;

namespace WindowsWorkflowAutomator.Tests;

public class LicensingRepositoryTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var ctx = new AppDbContext(options);
        ctx.Database.EnsureCreated();
        ctx.EnsureSchema();
        return ctx;
    }

    [Fact]
    public async Task SaveAndGet_LicenseInfo_Works()
    {
        using var ctx = CreateInMemoryContext();

        var repo = new LicenseRepository(ctx);

        var license = new LicenseInfo
        {
            LicenseKey = "WFA-PRO-ABCD-1234",
            Tier = "Premium",
            IsActive = true,
            ActivatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddYears(1),
            DeviceCount = 1,
            DeviceLimit = 1
        };

        await repo.SaveAsync(license);

        var fetched = await repo.GetAsync();

        Assert.NotNull(fetched);
        Assert.Equal(license.LicenseKey, fetched!.LicenseKey);
        Assert.Equal("Premium", fetched.Tier);
    }

    [Fact]
    public async Task Delete_RemovesLicense()
    {
        using var ctx = CreateInMemoryContext();

        var repo = new LicenseRepository(ctx);

        var license = new LicenseInfo { LicenseKey = "WFA-PRO-ABCD-1234", Tier = "Premium", IsActive = true, DeviceCount = 1, DeviceLimit = 1 };
        await repo.SaveAsync(license);

        var fetched = await repo.GetAsync();
        Assert.NotNull(fetched);

        await repo.DeleteAsync();

        var after = await repo.GetAsync();
        Assert.Null(after);
    }
}
