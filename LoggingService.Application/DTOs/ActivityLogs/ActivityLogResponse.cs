using LoggingService.Domain.Enums;
namespace LoggingService.Application.DTOs.ActivityLogs;

public class ActivityLogResponse
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string ActionType { get; set; } = default!;
    public string EntityType { get; set; } = default!;
    public string? EntityId { get; set; }
    public string ServiceName { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserRole { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? BranchId { get; set; }
    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime CreatedAt { get; set; }
}
