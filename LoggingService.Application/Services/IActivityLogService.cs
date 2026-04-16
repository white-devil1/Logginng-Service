using LoggingService.Application.DTOs.ActivityLogs;

namespace LoggingService.Application.Services;

public interface IActivityLogService
{
    Task<ActivityLogListResponse> GetLogsAsync(
        string? actionType, string? entityType,
        string? serviceName, string? userId, Guid? tenantId, Guid? branchId,
        DateTime? fromDate, DateTime? toDate, string? search,
        int page, int pageSize, string sortOrder, string sortBy, CancellationToken ct = default);
    Task<ActivityLogResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AvailableFiltersResponse> GetAvailableFiltersAsync(CancellationToken ct = default);
}
