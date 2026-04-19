using Microsoft.EntityFrameworkCore;
using LoggingService.Application.DTOs.LoginAudits;
using LoggingService.Application.Services;
using LoggingService.Domain.Entities;
using LoggingService.Domain.Enums;
using LoggingService.Infrastructure.Persistence;
namespace LoggingService.Infrastructure.Services.LoginAudits;

public class LoginAuditService : ILoginAuditService
{
    private readonly LoggingDbContext _db;
    public LoginAuditService(LoggingDbContext db) => _db = db;

    public async Task<LoginAuditListResponse> GetLogsAsync(
        LoginEventType? eventType, string? userId, string? email,
        Guid? tenantId, Guid? branchId, string? ipAddress,
        DateTime? fromDate, DateTime? toDate,
        int page, int pageSize, string sortOrder, string? sortBy = null, CancellationToken ct = default)
    {
        var q = _db.LoginAudits.AsQueryable();
        if (eventType.HasValue) q = q.Where(l => l.EventType == eventType.Value);
        if (!string.IsNullOrEmpty(userId)) q = q.Where(l => l.UserId == userId);
        if (!string.IsNullOrEmpty(email)) q = q.Where(l => l.Email.Contains(email));
        if (tenantId.HasValue) q = q.Where(l => l.TenantId == tenantId.Value);
        if (branchId.HasValue) q = q.Where(l => l.BranchId == branchId.Value);
        if (!string.IsNullOrEmpty(ipAddress)) q = q.Where(l => l.IpAddress == ipAddress);
        if (fromDate.HasValue) q = q.Where(l => l.Timestamp >= fromDate.Value);
        if (toDate.HasValue) q = q.Where(l => l.Timestamp <= toDate.Value);
        
        var total = await q.CountAsync(ct);
        
        // Apply database sorting for Timestamp
        if (string.IsNullOrEmpty(sortBy) || sortBy.Equals("Timestamp", StringComparison.OrdinalIgnoreCase))
        {
            q = sortOrder.ToLower() == "asc"
                ? q.OrderBy(l => l.Timestamp)
                : q.OrderByDescending(l => l.Timestamp);
            
            var logs = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            var enrichedLogs = await EnrichWithUserNames(logs, ct);
            return new LoginAuditListResponse
            { Logs = enrichedLogs, TotalCount = total, Page = page, PageSize = pageSize };
        }
        // Apply in-memory sorting for UserName
        else if (sortBy.Equals("UserName", StringComparison.OrdinalIgnoreCase))
        {
            var logs = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            var enrichedLogs = await EnrichWithUserNames(logs, ct);
            
            var sortedLogs = sortOrder.ToLower() == "asc"
                ? enrichedLogs.OrderBy(l => l.FullName).ToList()
                : enrichedLogs.OrderByDescending(l => l.FullName).ToList();
            
            return new LoginAuditListResponse
            { Logs = sortedLogs, TotalCount = total, Page = page, PageSize = pageSize };
        }
        else
        {
            // Default to Timestamp sorting
            q = sortOrder.ToLower() == "asc"
                ? q.OrderBy(l => l.Timestamp)
                : q.OrderByDescending(l => l.Timestamp);
            
            var logs = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            var enrichedLogs = await EnrichWithUserNames(logs, ct);
            return new LoginAuditListResponse
            { Logs = enrichedLogs, TotalCount = total, Page = page, PageSize = pageSize };
        }
    }

    private async Task<List<LoginAuditResponse>> EnrichWithUserNames(List<LoginAudit> logs, CancellationToken ct)
    {
        if (logs.Count == 0) return new List<LoginAuditResponse>();
        
        // Collect unique user IDs
        var userIds = logs.Where(l => !string.IsNullOrEmpty(l.UserId))
                          .Select(l => l.UserId!)
                          .Distinct()
                          .ToList();
        
        if (userIds.Count == 0)
        {
            return logs.Select(Map).ToList();
        }
        
        // Fetch user details from UserSync table
        var users = await _db.UserSyncs
            .Where(u => userIds.Contains(u.UserId))
            .ToListAsync(ct);
        
        var userDict = users.ToDictionary(u => u.UserId, u => u, StringComparer.OrdinalIgnoreCase);
        
        // Map with enrichment
        return logs.Select(l =>
        {
            var response = Map(l);
            if (!string.IsNullOrEmpty(l.UserId) && userDict.TryGetValue(l.UserId, out var user))
            {
                response.FirstName = user.FirstName;
                response.LastName = user.LastName;
                response.FullName = $"{user.FirstName ?? ""} {user.LastName ?? ""}".Trim();
                if (string.IsNullOrEmpty(response.FullName))
                {
                    response.FullName = user.UserName ?? "Unknown User";
                }
            }
            else
            {
                response.FullName = "Unknown User";
            }
            return response;
        }).ToList();
    }

    public async Task<LoginAuditResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var l = await _db.LoginAudits.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (l == null) return null;
        
        var response = Map(l);
        
        // Enrich single record with user name
        if (!string.IsNullOrEmpty(l.UserId))
        {
            var user = await _db.UserSyncs.FirstOrDefaultAsync(u => u.UserId == l.UserId, ct);
            if (user != null)
            {
                response.FirstName = user.FirstName;
                response.LastName = user.LastName;
                response.FullName = $"{user.FirstName ?? ""} {user.LastName ?? ""}".Trim();
                if (string.IsNullOrEmpty(response.FullName))
                {
                    response.FullName = user.UserName ?? "Unknown User";
                }
            }
            else
            {
                response.FullName = "Unknown User";
            }
        }
        else
        {
            response.FullName = "Unknown User";
        }
        
        return response;
    }

    public async Task<SummaryResponse> GetSummaryAsync(
        Guid? tenantId, Guid? branchId, bool isSuperAdmin, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekAgo = today.AddDays(-7);
        var monthAgo = today.AddDays(-30);

        var errQ = _db.ErrorLogs.AsQueryable();
        if (!isSuperAdmin && tenantId.HasValue)
            errQ = errQ.Where(e => e.TenantId == tenantId);

        var actQ = _db.ActivityLogs.AsQueryable();
        if (!isSuperAdmin && tenantId.HasValue)
            actQ = actQ.Where(a => a.TenantId == tenantId);

        var loginQ = _db.LoginAudits.AsQueryable();
        if (!isSuperAdmin && tenantId.HasValue)
            loginQ = loginQ.Where(l => l.TenantId == tenantId);

        return new SummaryResponse
        {
            ErrorLogs = new ErrorSummary
            {
                TotalToday = await errQ.CountAsync(e => e.Timestamp >= today, ct),
                TotalThisWeek = await errQ.CountAsync(e => e.Timestamp >= weekAgo, ct),
                TotalThisMonth = await errQ.CountAsync(e => e.Timestamp >= monthAgo, ct),
                ByCategory = await errQ.Where(e => e.Timestamp >= today)
                    .GroupBy(e => e.Category)
                    .ToDictionaryAsync(g => g.Key.ToString(), g => g.Count(), ct),
                BySource = await errQ.Where(e => e.Timestamp >= today)
                    .GroupBy(e => e.Source)
                    .ToDictionaryAsync(g => g.Key.ToString(), g => g.Count(), ct),
                ByService = await errQ.Where(e => e.Timestamp >= today)
                    .GroupBy(e => e.ServiceName)
                    .ToDictionaryAsync(g => g.Key, g => g.Count(), ct),
            },
            ActivityLogs = new ActivitySummary
            {
                TotalToday = await actQ.CountAsync(a => a.Timestamp >= today, ct),
                TotalThisWeek = await actQ.CountAsync(a => a.Timestamp >= weekAgo, ct),
                ByEntityType = await actQ.Where(a => a.Timestamp >= today)
                    .GroupBy(a => a.EntityType)
                    .ToDictionaryAsync(g => g.Key, g => g.Count(), ct),
                ByActionType = await actQ.Where(a => a.Timestamp >= today)
                    .GroupBy(a => a.ActionType)
                    .ToDictionaryAsync(g => g.Key, g => g.Count(), ct),
            },
            LoginAudits = new LoginSummary
            {
                TotalLoginsToday = await loginQ.CountAsync(l => l.Timestamp >= today && l.EventType == LoginEventType.Login, ct),
                TotalLogoutsToday = await loginQ.CountAsync(l => l.Timestamp >= today && l.EventType == LoginEventType.Logout, ct),
                TotalLoginsThisWeek = await loginQ.CountAsync(l => l.Timestamp >= weekAgo && l.EventType == LoginEventType.Login, ct),
                UniqueUsersToday = await loginQ
                    .Where(l => l.Timestamp >= today && l.EventType == LoginEventType.Login && l.UserId != null)
                    .Select(l => l.UserId).Distinct().CountAsync(ct),
            }
        };
    }

    private static LoginAuditResponse Map(LoginAudit l) => new()
    {
        Id = l.Id,
        Timestamp = l.Timestamp,
        EventType = l.EventType,
        UserId = l.UserId,
        Email = l.Email,
        TenantId = l.TenantId,
        BranchId = l.BranchId,
        IpAddress = l.IpAddress,
        UserAgent = l.UserAgent,
        DeviceType = l.DeviceType,
        CreatedAt = l.CreatedAt
    };
}
