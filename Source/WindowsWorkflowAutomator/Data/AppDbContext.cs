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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityLogEntry>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Level).HasMaxLength(32);
            entity.Property(x => x.Category).HasMaxLength(128);
            entity.Property(x => x.Message).HasMaxLength(2000);
        });
    }
}
