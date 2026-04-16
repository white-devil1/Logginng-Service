using MediatR;
using LoggingService.Application.DTOs.LoginAudits;
namespace LoggingService.Application.Commands.LoginAudits;

public class GetLoginAuditByIdCommand : IRequest<LoginAuditResponse>
{
    public Guid Id { get; set; }
    public Guid? CallerTenantId { get; set; }
    public bool IsSuperAdmin { get; set; }
}
