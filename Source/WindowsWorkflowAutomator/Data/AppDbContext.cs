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

    public DbSet<FileOrganizationRule> FileOrganizationRules => Set<FileOrganizationRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityLogEntry>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Level).HasMaxLength(32);
            entity.Property(x => x.Category).HasMaxLength(128);
            entity.Property(x => x.Message).HasMaxLength(2000);
        });

        modelBuilder.Entity<FileOrganizationRule>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Extension).HasMaxLength(256).IsRequired();
            entity.Property(x => x.DestinationFolder).HasMaxLength(1024).IsRequired();
            entity.Property(x => x.Action).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.RenamePattern).HasMaxLength(256);
        });
    }

    public void EnsureSchema()
    {
        Database.EnsureCreated();
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
    }
}
