using LoggingService.Domain.Common;
using LoggingService.Domain.Enums;
namespace LoggingService.Domain.Entities;

// Login success and logout only
public class LoginAudit : BaseEntity
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public LoginEventType EventType { get; set; }
    public string? UserId { get; set; }
    public string Email { get; set; } = default!;
    public Guid? TenantId { get; set; }
    public Guid? BranchId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DeviceType DeviceType { get; set; } = DeviceType.Unknown;
}
