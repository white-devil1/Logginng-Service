using LoggingService.Domain.Entities;
namespace LoggingService.Application.Services;

public interface IUserSyncService
{
    Task<UserSync?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task UpsertAsync(UserSync user, CancellationToken ct = default);
    Task MarkDeletedAsync(string userId, CancellationToken ct = default);
    Task MarkRestoredAsync(string userId, CancellationToken ct = default);
}
