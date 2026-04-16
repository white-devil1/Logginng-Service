using Microsoft.EntityFrameworkCore;
using LoggingService.Application.DTOs.ErrorLogs;
using LoggingService.Application.Services;
using LoggingService.Domain.Entities;
using LoggingService.Domain.Enums;
using LoggingService.Infrastructure.Persistence;
namespace LoggingService.Infrastructure.Services.ErrorLogs;

public class ErrorLogService : IErrorLogService
{
    private readonly LoggingDbContext _db;
    public ErrorLogService(LoggingDbContext db) => _db = db;

    public async Task<ErrorLogListResponse> GetLogsAsync(
        Severity? severity, LogSource? source, ErrorCategory? category,
        DateTime? fromDate, DateTime? toDate, string? search,
        int page, int pageSize, string sortOrder, string sortBy, CancellationToken ct = default)
    {
        var q = _db.ErrorLogs.AsQueryable();
        if (severity.HasValue) q = q.Where(e => e.Severity == severity.Value);
        if (source.HasValue) q = q.Where(e => e.Source == source.Value);
        if (category.HasValue) q = q.Where(e => e.Category == category.Value);
        if (fromDate.HasValue) q = q.Where(e => e.Timestamp >= fromDate.Value);
        if (toDate.HasValue) q = q.Where(e => e.Timestamp <= toDate.Value);
        if (!string.IsNullOrEmpty(search)) q = q.Where(e => e.Message.Contains(search));
        var total = await q.CountAsync(ct);

        var isDesc = sortOrder.ToLower() != "asc";
        q = sortBy.ToLower() switch
        {
            "severity" => isDesc
                ? q.OrderByDescending(e => e.Severity)
                : q.OrderBy(e => e.Severity),
            _ => isDesc
                ? q.OrderByDescending(e => e.Timestamp)
                : q.OrderBy(e => e.Timestamp)
        };

        var logs = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new ErrorLogListResponse
        { Logs = logs.Select(Map).ToList(), TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<ErrorLogResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var e = await _db.ErrorLogs.FirstOrDefaultAsync(x => x.Id == id, ct);
        return e == null ? null : Map(e);
    }

    private static ErrorLogResponse Map(ErrorLog e) => new()
    {
        Id = e.Id,
        Timestamp = e.Timestamp,
        Severity = e.Severity,
        Source = e.Source,
        Category = e.Category,
        ServiceName = e.ServiceName,
        Environment = e.Environment,
        Message = e.Message,
        StackTrace = e.StackTrace,
        RequestPath = e.RequestPath,
        RequestMethod = e.RequestMethod,
        StatusCode = e.StatusCode,
        UserId = e.UserId,
        UserEmail = e.UserEmail,
        TenantId = e.TenantId,
        BranchId = e.BranchId,
        IpAddress = e.IpAddress,
        UserAgent = e.UserAgent,
        AdditionalData = e.AdditionalData,
        CreatedAt = e.CreatedAt
    };
}
