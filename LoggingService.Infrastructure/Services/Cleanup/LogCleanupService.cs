using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using LoggingService.Infrastructure.Persistence;
namespace LoggingService.Infrastructure.Services.Cleanup;

// Runs daily at midnight UTC — deletes logs older than configured retention days
public class LogCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LogCleanupService> _logger;
    private readonly int _errorDays, _activityDays, _loginDays;

    public LogCleanupService(IServiceScopeFactory scopeFactory,
        IConfiguration cfg, ILogger<LogCleanupService> logger)
    {
        _scopeFactory = scopeFactory; _logger = logger;
        _errorDays = cfg.GetValue<int>("Retention:ErrorLogDays", 30);
        _activityDays = cfg.GetValue<int>("Retention:ActivityLogDays", 30);
        _loginDays = cfg.GetValue<int>("Retention:LoginAuditDays", 30);
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var delay = DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow;
            await Task.Delay(delay, ct);
            if (ct.IsCancellationRequested) break;
            await RunCleanupAsync(ct);
        }
    }

    private async Task RunCleanupAsync(CancellationToken ct)
    {
        _logger.LogInformation("LogCleanup: starting");
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LoggingDbContext>();
            var now = DateTime.UtcNow;

            var deleted = await db.ErrorLogs
                .Where(e => e.CreatedAt < now.AddDays(-_errorDays))
                .ExecuteDeleteAsync(ct);
            _logger.LogInformation("LogCleanup: deleted {N} error logs", deleted);

            deleted = await db.ActivityLogs
                .Where(a => a.CreatedAt < now.AddDays(-_activityDays))
                .ExecuteDeleteAsync(ct);
            _logger.LogInformation("LogCleanup: deleted {N} activity logs", deleted);

            deleted = await db.LoginAudits
                .Where(l => l.CreatedAt < now.AddDays(-_loginDays))
                .ExecuteDeleteAsync(ct);
            _logger.LogInformation("LogCleanup: deleted {N} login audits", deleted);
        }
        catch (Exception ex) { _logger.LogError(ex, "LogCleanup: failed"); }
    }
}
