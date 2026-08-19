using Microsoft.EntityFrameworkCore;
using WindowsWorkflowAutomator.Data;
using WindowsWorkflowAutomator.Models;

namespace WindowsWorkflowAutomator.Repositories;

public sealed class ActivityLogRepository : IActivityLogRepository
{
    private readonly AppDbContext _db;

    public ActivityLogRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(ActivityLogEntry entry, CancellationToken cancellationToken = default)
    {
        _db.ActivityLogs.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ActivityLogEntry>> GetRecentAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        return await _db.ActivityLogs
            .OrderByDescending(x => x.TimestampUtc)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
