using Microsoft.EntityFrameworkCore;
using LoggingService.Domain.Entities;
using LoggingService.Infrastructure.Persistence;
namespace LoggingService.Infrastructure.Services.Seeding;

public static class DatabaseSeeder
{
    // Same SuperAdmin as UserManagementService and CompanySettingsService
    private const string SuperAdminUserId = "e71a9f33-1832-4055-a852-7beb05ec44d0";

    public static async Task SeedAsync(LoggingDbContext context)
    {
        if (await context.UserSyncs.AnyAsync(u => u.UserId == SuperAdminUserId))
        {
            Console.WriteLine("   Logging already seeded. Skipping.");
            return;
        }
        context.UserSyncs.Add(new UserSync
        {
            UserId = SuperAdminUserId,
            TenantId = Guid.Parse("00000000-0000-0000-0000-000000000000"),
            BranchId = null,
            Email = "aneeshsolomon59@gmail.com",
            UserName = "aneeshsolomon59",
            FirstName = "Super",
            LastName = "Admin",
            IsActive = true,
            IsDeleted = false,
            IsSuperAdmin = true,
            Roles = "[\"Super Admin\"]",
            LastSyncedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        Console.WriteLine("OK SuperAdmin seeded into LoggingService UserSync.");
        Console.WriteLine($"   UserId: {SuperAdminUserId}");
        Console.WriteLine("   Email:  aneeshsolomon59@gmail.com");
    }
}
