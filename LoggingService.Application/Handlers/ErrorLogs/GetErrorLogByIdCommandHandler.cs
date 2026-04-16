using MediatR;
using LoggingService.Application.Commands.ErrorLogs;
using LoggingService.Application.Common.Exceptions;
using LoggingService.Application.DTOs.ErrorLogs;
using LoggingService.Application.Services;
namespace LoggingService.Application.Handlers.ErrorLogs;

public class GetErrorLogByIdCommandHandler : IRequestHandler<GetErrorLogByIdCommand, ErrorLogResponse>
{
    private readonly IErrorLogService _svc;
    public GetErrorLogByIdCommandHandler(IErrorLogService svc) => _svc = svc;
    public async Task<ErrorLogResponse> Handle(GetErrorLogByIdCommand r, CancellationToken ct)
    {
        var log = await _svc.GetByIdAsync(r.Id, ct);
        if (log == null) throw new NotFoundException("ErrorLog", r.Id);
        return log;
    }
}
