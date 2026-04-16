using MediatR;
using LoggingService.Application.Commands.ActivityLogs;
using LoggingService.Application.DTOs.ActivityLogs;
using LoggingService.Application.Services;
namespace LoggingService.Application.Handlers.ActivityLogs;

public class ListActivityLogsCommandHandler : IRequestHandler<ListActivityLogsCommand, ActivityLogListResponse>
{
    private readonly IActivityLogService _svc;
    public ListActivityLogsCommandHandler(IActivityLogService svc) => _svc = svc;
    public async Task<ActivityLogListResponse> Handle(ListActivityLogsCommand r, CancellationToken ct)
        => await _svc.GetLogsAsync(r.ActionType, r.EntityType, r.ServiceName,
              r.UserId, r.TenantId, r.BranchId, r.FromDate, r.ToDate,
              r.Search, r.Page, r.PageSize, r.SortOrder, r.SortBy, ct);
}
