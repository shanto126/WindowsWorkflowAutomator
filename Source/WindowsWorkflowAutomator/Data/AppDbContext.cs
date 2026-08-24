using Microsoft.EntityFrameworkCore;
using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<ActivityLogEntry> ActivityLogs => Set<ActivityLogEntry>();

    public DbSet<FileOrganizationRule> FileOrganizationRules =>
        Set<FileOrganizationRule>();

    public DbSet<GitHubRepository> GitHubRepositories =>
        Set<GitHubRepository>();

    public DbSet<Workflow> Workflows =>
        Set<Workflow>();

    public DbSet<WorkflowAction> WorkflowActions =>
        Set<WorkflowAction>();

    public DbSet<SocialPost> SocialPosts =>
        Set<SocialPost>();

    public DbSet<PostImage> PostImages =>
        Set<PostImage>();

    public DbSet<SocialAccount> SocialAccounts =>
        Set<SocialAccount>();

    public DbSet<LicenseInfo> LicenseInfos =>
        Set<LicenseInfo>();

    public DbSet<ScheduledTask> ScheduledTasks => Set<ScheduledTask>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityLogEntry>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Level)
                .HasMaxLength(32);

            entity.Property(x => x.Category)
                .HasMaxLength(128);

            entity.Property(x => x.Message)
                .HasMaxLength(2000);
        });

        modelBuilder.Entity<FileOrganizationRule>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Extension)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(x => x.DestinationFolder)
                .HasMaxLength(1024)
                .IsRequired();

            entity.Property(x => x.Action)
                .HasConversion<string>()
                .HasMaxLength(32);

            entity.Property(x => x.RenamePattern)
                .HasMaxLength(256);
        });

        modelBuilder.Entity<GitHubRepository>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.LocalPath)
                .HasMaxLength(1024)
                .IsRequired();

            entity.Property(x => x.RemoteUrl)
                .HasMaxLength(1024)
                .IsRequired();

            entity.Property(x => x.Branch)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(x => x.CommitMessageTemplate)
                .HasMaxLength(300)
                .IsRequired();

            entity.Property(x => x.SyncMode)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(x => x.InactivitySeconds)
                .IsRequired();

            entity.Property(x => x.UpdatedAtUtc)
                .IsRequired();
        });

        modelBuilder.Entity<Workflow>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(1000);

            entity.HasMany(x => x.Actions)
                .WithOne(x => x.Workflow)
                .HasForeignKey(x => x.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ScheduledTask>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ScheduleType).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.ScheduledAt).IsRequired();
            entity.Property(x => x.WeeklyDay).HasConversion<string>().HasMaxLength(16);
            entity.HasOne(x => x.Workflow)
                .WithMany()
                .HasForeignKey(x => x.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkflowAction>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Type)
                .HasConversion<string>()
                .HasMaxLength(64);

            entity.Property(x => x.Target)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(x => x.Arguments)
                .HasMaxLength(1000);

            entity.Property(x => x.Order)
                .HasColumnName("SortOrder");
        });

        modelBuilder.Entity<SocialPost>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Platform)
                .HasConversion<string>()
                .HasMaxLength(32);

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32);

            entity.Property(x => x.CaptionMode)
                .HasConversion<string>()
                .HasMaxLength(32);

            entity.Property(x => x.CaptionInput)
                .HasMaxLength(4000);

            entity.Property(x => x.ResolvedCaption)
                .HasMaxLength(4000);

            entity.Property(x => x.ErrorMessage)
                .HasMaxLength(2000);

            entity.HasMany(x => x.Images)
                .WithOne(x => x.SocialPost)
                .HasForeignKey(x => x.SocialPostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PostImage>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.FilePath)
                .HasMaxLength(1024)
                .IsRequired();

            entity.HasIndex(x => new
            {
                x.SocialPostId,
                x.Order
            });
        });

        modelBuilder.Entity<SocialAccount>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Platform)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(x => x.DisplayName)
                .HasMaxLength(200)
                .IsRequired();

            entity.HasIndex(x => x.Platform)
                .IsUnique();
        });

        modelBuilder.Entity<LicenseInfo>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.LicenseKey)
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(x => x.Tier)
                .HasMaxLength(32)
                .IsRequired();

            entity.HasIndex(x => x.LicenseKey)
                .IsUnique();
        });
    }

    public void EnsureSchema()
    {
        Database.EnsureCreated();

        Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS LicenseInfos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LicenseKey TEXT NOT NULL,
                Tier TEXT NOT NULL,
                IsActive INTEGER NOT NULL,
                ActivatedAtUtc TEXT NULL,
                ExpiresAtUtc TEXT NULL,
                DeviceCount INTEGER NOT NULL,
                DeviceLimit INTEGER NOT NULL
            );
            """);

        Database.ExecuteSqlRaw(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS IX_LicenseInfos_LicenseKey
            ON LicenseInfos (LicenseKey);
            """);

        Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS FileOrganizationRules (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Extension TEXT NOT NULL,
                DestinationFolder TEXT NOT NULL,
                Action TEXT NOT NULL,
                IsEnabled INTEGER NOT NULL,
                RenamePattern TEXT NULL
            );
            """);

        Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS Workflows (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Description TEXT NOT NULL,
                IsEnabled INTEGER NOT NULL
            );
            """);

        Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS GitHubRepositories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LocalPath TEXT NOT NULL,
                RemoteUrl TEXT NOT NULL,
                Branch TEXT NOT NULL,
                CommitMessageTemplate TEXT NOT NULL,
                SyncMode TEXT NOT NULL DEFAULT 'Manual',
                InactivitySeconds INTEGER NOT NULL DEFAULT 30,
                UpdatedAtUtc TEXT NOT NULL
            );
            """);

        try
        {
            Database.ExecuteSqlRaw(
                "ALTER TABLE GitHubRepositories ADD COLUMN SyncMode TEXT NOT NULL DEFAULT 'Manual';");
        }
        catch
        {
            // Column already exists.
        }

        try
        {
            Database.ExecuteSqlRaw(
                "ALTER TABLE GitHubRepositories ADD COLUMN InactivitySeconds INTEGER NOT NULL DEFAULT 30;");
        }
        catch
        {
            // Column already exists.
        }

        Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS WorkflowActions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                WorkflowId INTEGER NOT NULL,
                Type TEXT NOT NULL,
                Target TEXT NOT NULL,
                Arguments TEXT NULL,
                SortOrder INTEGER NOT NULL,
                FOREIGN KEY(WorkflowId)
                    REFERENCES Workflows(Id)
                    ON DELETE CASCADE
            );
            """);
        Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS ScheduledTasks (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                WorkflowId INTEGER NOT NULL,
                ScheduleType TEXT NOT NULL,
                ScheduledAt TEXT NOT NULL,
                WeeklyDay TEXT NULL,
                IsEnabled INTEGER NOT NULL,
                LastRunAtUtc TEXT NULL,
                FOREIGN KEY(WorkflowId) REFERENCES Workflows(Id) ON DELETE CASCADE
            );
            """);
    }
}