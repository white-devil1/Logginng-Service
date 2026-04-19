using MediatR;
using LoggingService.Application.DTOs.LoginAudits;
using LoggingService.Domain.Enums;
namespace LoggingService.Application.Commands.LoginAudits;

public class ListLoginAuditsCommand : IRequest<LoginAuditListResponse>
{
    public LoginEventType? EventType { get; set; }
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? BranchId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortOrder { get; set; } = "desc";
    public string? SortBy { get; set; } = "Timestamp";
}
