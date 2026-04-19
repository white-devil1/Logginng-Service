using LoggingService.Domain.Enums;
namespace LoggingService.Application.DTOs.LoginAudits;

public class LoginAuditResponse
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public LoginEventType EventType { get; set; }
    public string? UserId { get; set; }
    public string Email { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? BranchId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DeviceType DeviceType { get; set; }
    public DateTime CreatedAt { get; set; }
}
