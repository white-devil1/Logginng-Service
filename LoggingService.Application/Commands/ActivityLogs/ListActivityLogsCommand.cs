using MediatR;
using LoggingService.Application.DTOs.ActivityLogs;
using LoggingService.Domain.Enums;
namespace LoggingService.Application.Commands.ActivityLogs;

public class ListActivityLogsCommand : IRequest<ActivityLogListResponse>
{
    public string? ActionType { get; set; }
    public string? EntityType { get; set; }
    public string? ServiceName { get; set; }
    public string? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? BranchId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortOrder { get; set; } = "desc";
    public string SortBy { get; set; } = "Timestamp";
}
