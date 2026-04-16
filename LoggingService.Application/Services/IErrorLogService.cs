using LoggingService.Application.DTOs.ErrorLogs;
using LoggingService.Domain.Enums;
namespace LoggingService.Application.Services;

public interface IErrorLogService
{
    Task<ErrorLogListResponse> GetLogsAsync(
        Severity? severity, LogSource? source, ErrorCategory? category,
        DateTime? fromDate, DateTime? toDate, string? search,
        int page, int pageSize, string sortOrder, string sortBy, CancellationToken ct = default);
    Task<ErrorLogResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
