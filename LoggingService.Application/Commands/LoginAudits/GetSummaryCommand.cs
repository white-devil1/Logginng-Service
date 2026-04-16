using MediatR;
using LoggingService.Application.DTOs.LoginAudits;
namespace LoggingService.Application.Commands.LoginAudits;

public class GetSummaryCommand : IRequest<SummaryResponse>
{
    public Guid? TenantId { get; set; }
    public Guid? BranchId { get; set; }
    public bool IsSuperAdmin { get; set; }
}
