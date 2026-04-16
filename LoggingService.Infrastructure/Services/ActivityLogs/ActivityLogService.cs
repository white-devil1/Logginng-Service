using Microsoft.EntityFrameworkCore;
using LoggingService.Application.DTOs.ActivityLogs;
using LoggingService.Application.Services;
using LoggingService.Domain.Entities;
using LoggingService.Infrastructure.Persistence;
namespace LoggingService.Infrastructure.Services.ActivityLogs;

public class ActivityLogService : IActivityLogService
{
    private readonly LoggingDbContext _db;
    public ActivityLogService(LoggingDbContext db) => _db = db;

    public async Task<ActivityLogListResponse> GetLogsAsync(
        string? actionType, string? entityType,
        string? serviceName, string? userId, Guid? tenantId, Guid? branchId,
        DateTime? fromDate, DateTime? toDate, string? search,
        int page, int pageSize, string sortOrder, string sortBy, CancellationToken ct = default)
    {
        var q = _db.ActivityLogs.AsQueryable();
        if (!string.IsNullOrEmpty(actionType)) q = q.Where(a => a.ActionType == actionType);
        if (!string.IsNullOrEmpty(entityType)) q = q.Where(a => a.EntityType == entityType);
        if (!string.IsNullOrEmpty(serviceName)) q = q.Where(a => a.ServiceName == serviceName);
        if (!string.IsNullOrEmpty(userId)) q = q.Where(a => a.UserId == userId);
        if (tenantId.HasValue) q = q.Where(a => a.TenantId == tenantId.Value);
        if (branchId.HasValue) q = q.Where(a => a.BranchId == branchId.Value);
        if (fromDate.HasValue) q = q.Where(a => a.Timestamp >= fromDate.Value);
        if (toDate.HasValue) q = q.Where(a => a.Timestamp <= toDate.Value);
        if (!string.IsNullOrEmpty(search)) q = q.Where(a => a.Description.Contains(search));
        var total = await q.CountAsync(ct);

        var isDesc = sortOrder.ToLower() != "asc";
        q = sortBy.ToLower() switch
        {
            "actiontype" => isDesc
                ? q.OrderByDescending(a => a.ActionType)
                : q.OrderBy(a => a.ActionType),
            "entitytype" => isDesc
                ? q.OrderByDescending(a => a.EntityType)
                : q.OrderBy(a => a.EntityType),
            "userid" => isDesc
                ? q.OrderByDescending(a => a.UserId)
                : q.OrderBy(a => a.UserId),
            _ => isDesc
                ? q.OrderByDescending(a => a.Timestamp)
                : q.OrderBy(a => a.Timestamp)
        };

        var logs = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new ActivityLogListResponse
        { Logs = logs.Select(Map).ToList(), TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<ActivityLogResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var a = await _db.ActivityLogs.FirstOrDefaultAsync(x => x.Id == id, ct);
        return a == null ? null : Map(a);
    }

    public async Task<AvailableFiltersResponse> GetAvailableFiltersAsync(CancellationToken ct = default)
    {
        var actionTypes = await _db.ActivityLogs
            .Where(a => a.ActionType != null)
            .Select(a => a.ActionType!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(ct);

        var entityTypes = await _db.ActivityLogs
            .Where(a => a.EntityType != null)
            .Select(a => a.EntityType!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(ct);

        return new AvailableFiltersResponse
        { ActionTypes = actionTypes, EntityTypes = entityTypes };
    }

    private static ActivityLogResponse Map(ActivityLog a) => new()
    {
        Id = a.Id,
        Timestamp = a.Timestamp,
        ActionType = a.ActionType,
        EntityType = a.EntityType,
        EntityId = a.EntityId,
        ServiceName = a.ServiceName,
        Description = a.Description,
        OldValues = a.OldValues,
        NewValues = a.NewValues,
        UserId = a.UserId,
        UserEmail = a.UserEmail,
        UserRole = a.UserRole,
        TenantId = a.TenantId,
        BranchId = a.BranchId,
        IpAddress = a.IpAddress,
        CorrelationId = a.CorrelationId,
        CreatedAt = a.CreatedAt
    };
}
