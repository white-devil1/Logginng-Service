using MediatR;
using LoggingService.Application.Commands.LoginAudits;
using LoggingService.Application.Common.Exceptions;
using LoggingService.Application.DTOs.LoginAudits;
using LoggingService.Application.Services;
namespace LoggingService.Application.Handlers.LoginAudits;

public class GetLoginAuditByIdCommandHandler : IRequestHandler<GetLoginAuditByIdCommand, LoginAuditResponse>
{
    private readonly ILoginAuditService _svc;
    public GetLoginAuditByIdCommandHandler(ILoginAuditService svc) => _svc = svc;
    public async Task<LoginAuditResponse> Handle(GetLoginAuditByIdCommand r, CancellationToken ct)
    {
        var log = await _svc.GetByIdAsync(r.Id, ct);
        if (log == null) throw new NotFoundException("LoginAudit", r.Id);
        if (!r.IsSuperAdmin && log.TenantId != r.CallerTenantId)
            throw new UnauthorizedException("You do not have access to this log.");
        return log;
    }
}
