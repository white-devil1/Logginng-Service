using Microsoft.EntityFrameworkCore;
using LoggingService.Application.Services;
using LoggingService.Domain.Entities;
using LoggingService.Infrastructure.Persistence;
namespace LoggingService.Infrastructure.Services.UserSyncs;

public class UserSyncService : IUserSyncService
{
    private readonly LoggingDbContext _db;
    public UserSyncService(LoggingDbContext db) => _db = db;

    public async Task<UserSync?> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => await _db.UserSyncs.FirstOrDefaultAsync(u => u.UserId == userId, ct);

    public async Task UpsertAsync(UserSync user, CancellationToken ct = default)
    {
        var existing = await _db.UserSyncs
            .FirstOrDefaultAsync(u => u.UserId == user.UserId, ct);
        if (existing == null)
            _db.UserSyncs.Add(user);
        else
        {
            existing.Email = user.Email; existing.UserName = user.UserName;
            existing.FirstName = user.FirstName; existing.LastName = user.LastName;
            existing.TenantId = user.TenantId; existing.BranchId = user.BranchId;
            existing.IsActive = user.IsActive; existing.IsDeleted = user.IsDeleted;
            existing.IsSuperAdmin = user.IsSuperAdmin; existing.Roles = user.Roles;
            existing.LastSyncedAt = DateTime.UtcNow; existing.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkDeletedAsync(string userId, CancellationToken ct = default)
    {
        var u = await _db.UserSyncs.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (u == null) return;
        u.IsDeleted = true; u.IsActive = false;
        u.LastSyncedAt = u.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkRestoredAsync(string userId, CancellationToken ct = default)
    {
        var u = await _db.UserSyncs.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (u == null) return;
        u.IsDeleted = false; u.IsActive = true;
        u.LastSyncedAt = u.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
