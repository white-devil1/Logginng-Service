using MediatR;
using LoggingService.Application.Commands.LoginAudits;
using LoggingService.Application.DTOs.LoginAudits;
using LoggingService.Application.Services;
namespace LoggingService.Application.Handlers.LoginAudits;

public class GetSummaryCommandHandler : IRequestHandler<GetSummaryCommand, SummaryResponse>
{
    private readonly ILoginAuditService _svc;
    public GetSummaryCommandHandler(ILoginAuditService svc) => _svc = svc;
    public async Task<SummaryResponse> Handle(GetSummaryCommand r, CancellationToken ct)
        => await _svc.GetSummaryAsync(r.TenantId, r.BranchId, r.IsSuperAdmin, ct);
}
