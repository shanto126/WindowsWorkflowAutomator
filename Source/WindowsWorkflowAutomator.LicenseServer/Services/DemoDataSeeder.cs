using Microsoft.EntityFrameworkCore;
using WindowsWorkflowAutomator.LicenseServer.Data;
using WindowsWorkflowAutomator.LicenseServer.Models;

namespace WindowsWorkflowAutomator.LicenseServer.Services;

public static class DemoDataSeeder
{
    public static async Task EnsureSchemaAsync(LicenseDbContext db)
    {
        var database = db.Database;
        await database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER NOT NULL CONSTRAINT PK_Users PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Email TEXT NOT NULL,
                PasswordHash TEXT NOT NULL DEFAULT '',
                Role TEXT NOT NULL DEFAULT 'User',
                CreatedAt TEXT NOT NULL
            );
            """);
        try
        {
            await database.ExecuteSqlRawAsync("ALTER TABLE Users ADD COLUMN PasswordHash TEXT NOT NULL DEFAULT '';");
        }
        catch (Microsoft.Data.Sqlite.SqliteException)
        {
        }

        try
        {
            await database.ExecuteSqlRawAsync("ALTER TABLE Users ADD COLUMN Role TEXT NOT NULL DEFAULT 'User';");
        }
        catch (Microsoft.Data.Sqlite.SqliteException)
        {
        }
        await database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS SubscriptionPlans (
                Id INTEGER NOT NULL CONSTRAINT PK_SubscriptionPlans PRIMARY KEY AUTOINCREMENT,
                PlanName TEXT NOT NULL,
                Price TEXT NOT NULL,
                DurationDays INTEGER NOT NULL
            );
            """);
        await database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS Payments (
                Id INTEGER NOT NULL CONSTRAINT PK_Payments PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Amount TEXT NOT NULL,
                PaymentMethod TEXT NOT NULL,
                TransactionId TEXT NOT NULL,
                Status TEXT NOT NULL,
                PaymentDate TEXT NOT NULL,
                CONSTRAINT FK_Payments_Users_UserId FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE RESTRICT
            );
            """);
        await database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS Subscriptions (
                Id INTEGER NOT NULL CONSTRAINT PK_Subscriptions PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                PlanId INTEGER NOT NULL,
                StartDate TEXT NOT NULL,
                EndDate TEXT NOT NULL,
                Status TEXT NOT NULL,
                CONSTRAINT FK_Subscriptions_Users_UserId FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE RESTRICT,
                CONSTRAINT FK_Subscriptions_SubscriptionPlans_PlanId FOREIGN KEY (PlanId) REFERENCES SubscriptionPlans (Id) ON DELETE RESTRICT
            );
            """);
        await database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS Licenses (
                Id INTEGER NOT NULL CONSTRAINT PK_Licenses PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                LicenseKey TEXT NOT NULL,
                PlanName TEXT NOT NULL,
                IsActive INTEGER NOT NULL,
                ExpiryDate TEXT NOT NULL,
                CONSTRAINT FK_Licenses_Users_UserId FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE RESTRICT
            );
            """);
        await database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_Email ON Users (Email);");
        await database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_SubscriptionPlans_PlanName ON SubscriptionPlans (PlanName);");
        await database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_Payments_TransactionId ON Payments (TransactionId);");
        await database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_Licenses_LicenseKey ON Licenses (LicenseKey);");
    }

    public static async Task SeedAsync(LicenseDbContext db)
    {
        var demoUser = await db.Users.FirstOrDefaultAsync(
            x => x.Email == "demo@windowsworkflowautomator.local");
        if (demoUser is null)
        {
            demoUser = new User
            {
                Name = "Demo User",
                Email = "demo@windowsworkflowautomator.local",
                PasswordHash = DemoPasswordHasher.Hash("demo123"),
                Role = "User",
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Users.Add(demoUser);
        }
        else
        {
            demoUser.PasswordHash = DemoPasswordHasher.Hash("demo123");
            demoUser.Role = "User";
        }

        var adminUser = await db.Users.FirstOrDefaultAsync(
            x => x.Email == "admin@windowsworkflowautomator.local");
        if (adminUser is null)
        {
            db.Users.Add(new User
            {
                Name = "Demo Admin",
                Email = "admin@windowsworkflowautomator.local",
                PasswordHash = DemoPasswordHasher.Hash("admin123"),
                Role = "Admin",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        if (!await db.SubscriptionPlans.AnyAsync())
        {
            db.SubscriptionPlans.AddRange(
                new SubscriptionPlan
                {
                    PlanName = "Free",
                    Price = 0,
                    DurationDays = 30
                },
                new SubscriptionPlan
                {
                    PlanName = "Basic",
                    Price = 499,
                    DurationDays = 30
                },
                new SubscriptionPlan
                {
                    PlanName = "Premium",
                    Price = 999,
                    DurationDays = 365
                });
        }

        await db.SaveChangesAsync();
    }
}
