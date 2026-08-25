using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WindowsWorkflowAutomator.Data;
using WindowsWorkflowAutomator.Licensing;
using WindowsWorkflowAutomator.Repositories;
using WindowsWorkflowAutomator.Models;
using Xunit;

namespace WindowsWorkflowAutomator.Tests;

public class LicensingTests
{
    private sealed class TestScope : IServiceScope
    {
        public TestScope(IServiceProvider provider) => ServiceProvider = provider;
        public IServiceProvider ServiceProvider { get; }
        public void Dispose() { }
    }

    private sealed class TestScopeFactory : IServiceScopeFactory
    {
        private readonly IServiceProvider _provider;
        public TestScopeFactory(IServiceProvider provider) => _provider = provider;
        public IServiceScope CreateScope() => new TestScope(_provider);
    }

    [Fact]
    public async Task LicenseRepository_Save_Get_Delete_Works()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn)
            .Options;

        using (var db = new AppDbContext(options))
        {
            db.EnsureSchema();
        }

        using (var db = new AppDbContext(options))
        {
            var repo = new LicenseRepository(db);

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
            var saved = await repo.GetAsync();
            Assert.NotNull(saved);
            Assert.Equal("WFA-PRO-ABCD-1234", saved!.LicenseKey);

            await repo.DeleteAsync();
            var afterDelete = await repo.GetAsync();
            Assert.Null(afterDelete);
        }
    }

    [Fact]
    public async Task LicenseService_Activate_Validate_Deactivate_Works()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn)
            .Options;

        using (var db = new AppDbContext(options))
        {
            db.EnsureSchema();
        }

        using (var db = new AppDbContext(options))
        {
            var repo = new LicenseRepository(db);

            var services = new ServiceCollection();
            services.AddSingleton<ILicenseRepository>(repo);
            var provider = services.BuildServiceProvider();
            var scopeFactory = new TestScopeFactory(provider);

            var svc = new LicenseService(scopeFactory);

            var activated = await svc.ActivateAsync("WFA-PRO-ABCD-1234");
            Assert.True(activated);

            var valid = await svc.ValidateAsync();
            Assert.True(valid);

            await svc.DeactivateAsync();
            var validAfter = await svc.ValidateAsync();
            Assert.False(validAfter);
        }
    }
}
