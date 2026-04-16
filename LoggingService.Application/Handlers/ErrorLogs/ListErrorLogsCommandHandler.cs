using MediatR;
using LoggingService.Application.Commands.ErrorLogs;
using LoggingService.Application.DTOs.ErrorLogs;
using LoggingService.Application.Services;
namespace LoggingService.Application.Handlers.ErrorLogs;

public class ListErrorLogsCommandHandler : IRequestHandler<ListErrorLogsCommand, ErrorLogListResponse>
{
    private readonly IErrorLogService _svc;
    public ListErrorLogsCommandHandler(IErrorLogService svc) => _svc = svc;
    public async Task<ErrorLogListResponse> Handle(ListErrorLogsCommand r, CancellationToken ct)
        => await _svc.GetLogsAsync(r.Severity, r.Source, r.Category,
              r.FromDate, r.ToDate, r.Search,
              r.Page, r.PageSize, r.SortOrder, r.SortBy, ct);
}
