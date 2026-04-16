namespace LoggingService.Domain.Entities;

// Identical to CompanySettingsService UserSync
// Primary key is string UserId — not a BaseEntity
public class UserSync
{
    public string UserId { get; set; } = default!;
    public Guid TenantId { get; set; }
    public Guid? BranchId { get; set; }
    public string Email { get; set; } = default!;
    public string UserName { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public bool IsSuperAdmin { get; set; } = false;
    public string Roles { get; set; } = "[]";
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
