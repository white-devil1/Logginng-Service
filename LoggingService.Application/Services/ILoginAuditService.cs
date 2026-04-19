using LoggingService.Application.DTOs.LoginAudits;
using LoggingService.Domain.Enums;
namespace LoggingService.Application.Services;

public interface ILoginAuditService
{
    Task<LoginAuditListResponse> GetLogsAsync(
        LoginEventType? eventType, string? userId, string? email,
        Guid? tenantId, Guid? branchId, string? ipAddress,
        DateTime? fromDate, DateTime? toDate,
        int page, int pageSize, string sortOrder, string? sortBy = null, CancellationToken ct = default);
    Task<LoginAuditResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<SummaryResponse> GetSummaryAsync(
        Guid? tenantId, Guid? branchId, bool isSuperAdmin, CancellationToken ct = default);
}
