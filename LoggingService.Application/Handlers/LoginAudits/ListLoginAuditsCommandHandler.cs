using MediatR;
using LoggingService.Application.Commands.LoginAudits;
using LoggingService.Application.DTOs.LoginAudits;
using LoggingService.Application.Services;
namespace LoggingService.Application.Handlers.LoginAudits;

public class ListLoginAuditsCommandHandler : IRequestHandler<ListLoginAuditsCommand, LoginAuditListResponse>
{
    private readonly ILoginAuditService _svc;
    public ListLoginAuditsCommandHandler(ILoginAuditService svc) => _svc = svc;
    public async Task<LoginAuditListResponse> Handle(ListLoginAuditsCommand r, CancellationToken ct)
        => await _svc.GetLogsAsync(r.EventType, r.UserId, r.Email,
              r.TenantId, r.BranchId, r.IpAddress, r.FromDate, r.ToDate,
              r.Page, r.PageSize, r.SortOrder, ct);
}
