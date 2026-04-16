using LoggingService.Domain.Common;
using LoggingService.Domain.Enums;
namespace LoggingService.Domain.Entities;

public class ErrorLog : BaseEntity
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Severity Severity { get; set; } = Severity.Error;
    public LogSource Source { get; set; } = LogSource.Backend;
    public ErrorCategory Category { get; set; } = ErrorCategory.ServerError;
    public string ServiceName { get; set; } = default!;
    public string Environment { get; set; } = "Development";
    public string Message { get; set; } = default!;
    public string? StackTrace { get; set; }
    public string? RequestPath { get; set; }
    public string? RequestMethod { get; set; }
    public int? StatusCode { get; set; }
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? BranchId { get; set; }
    public string? CorrelationId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? AdditionalData { get; set; }
}
