using MediatR;
using LoggingService.Application.DTOs.ErrorLogs;
namespace LoggingService.Application.Commands.ErrorLogs;

public class GetErrorLogByIdCommand : IRequest<ErrorLogResponse> { public Guid Id { get; set; } }
