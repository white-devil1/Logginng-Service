using LoggingService.Domain.Enums;
namespace LoggingService.Application.DTOs.ErrorLogs;

public class ErrorLogResponse
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public Severity Severity { get; set; }
    public LogSource Source { get; set; }
    public ErrorCategory Category { get; set; }
    public string ServiceName { get; set; } = default!;
    public string Environment { get; set; } = default!;
    public string Message { get; set; } = default!;
    public string? StackTrace { get; set; }
    public string? RequestPath { get; set; }
    public string? RequestMethod { get; set; }
    public int? StatusCode { get; set; }
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? BranchId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? AdditionalData { get; set; }
    public DateTime CreatedAt { get; set; }
}
