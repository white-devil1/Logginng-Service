using MediatR;
using LoggingService.Application.Commands.ActivityLogs;
using LoggingService.Application.Common.Exceptions;
using LoggingService.Application.DTOs.ActivityLogs;
using LoggingService.Application.Services;
namespace LoggingService.Application.Handlers.ActivityLogs;

public class GetActivityLogByIdCommandHandler : IRequestHandler<GetActivityLogByIdCommand, ActivityLogResponse>
{
    private readonly IActivityLogService _svc;
    public GetActivityLogByIdCommandHandler(IActivityLogService svc) => _svc = svc;
    public async Task<ActivityLogResponse> Handle(GetActivityLogByIdCommand r, CancellationToken ct)
    {
        var log = await _svc.GetByIdAsync(r.Id, ct);
        if (log == null) throw new NotFoundException("ActivityLog", r.Id);
        if (!r.IsSuperAdmin && log.TenantId != r.CallerTenantId)
            throw new UnauthorizedException("You do not have access to this log.");
        return log;
    }
}
