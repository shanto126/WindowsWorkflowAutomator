using Microsoft.EntityFrameworkCore;
using WindowsWorkflowAutomator.LicenseServer.Models;

namespace WindowsWorkflowAutomator.LicenseServer.Data;

public sealed class LicenseDbContext(DbContextOptions<LicenseDbContext> options)
    : DbContext(options)
{
    public DbSet<LicenseValidationRecord> LicenseValidations => Set<LicenseValidationRecord>();

    public DbSet<User> Users => Set<User>();

    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<License> Licenses => Set<License>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<SubscriptionPlan>().HasIndex(x => x.PlanName).IsUnique();
        modelBuilder.Entity<Payment>().HasIndex(x => x.TransactionId).IsUnique();
        modelBuilder.Entity<License>().HasIndex(x => x.LicenseKey).IsUnique();
        modelBuilder.Entity<Payment>().Property(x => x.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<SubscriptionPlan>().Property(x => x.Price).HasPrecision(18, 2);

        modelBuilder.Entity<Payment>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Subscription>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Subscription>()
            .HasOne(x => x.Plan)
            .WithMany()
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<License>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}