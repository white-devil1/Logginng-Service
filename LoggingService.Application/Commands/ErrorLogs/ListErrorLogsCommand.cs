using MediatR;
using LoggingService.Application.DTOs.ErrorLogs;
using LoggingService.Domain.Enums;
namespace LoggingService.Application.Commands.ErrorLogs;

public class ListErrorLogsCommand : IRequest<ErrorLogListResponse>
{
    public Severity? Severity { get; set; }
    public LogSource? Source { get; set; }
    public ErrorCategory? Category { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortOrder { get; set; } = "desc";
    public string SortBy { get; set; } = "Timestamp";
}
