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
    public DbSet<Workflow> Workflows => Set<Workflow>();
    public DbSet<WorkflowAction> WorkflowActions => Set<WorkflowAction>();
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
        modelBuilder.Entity<Workflow>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.HasMany(x => x.Actions)
                .WithOne(x => x.Workflow)
                .HasForeignKey(x => x.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<WorkflowAction>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(64);
            entity.Property(x => x.Target).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.Arguments).HasMaxLength(1000);
            entity.Property(x => x.Order).HasColumnName("SortOrder");
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
            CREATE TABLE IF NOT EXISTS WorkflowActions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                WorkflowId INTEGER NOT NULL,
                Type TEXT NOT NULL,
                Target TEXT NOT NULL,
                Arguments TEXT NULL,
                SortOrder INTEGER NOT NULL,
                FOREIGN KEY(WorkflowId) REFERENCES Workflows(Id) ON DELETE CASCADE
            );
            """);
    }
}