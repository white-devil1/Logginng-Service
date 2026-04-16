using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LoggingService.Application.Services;
using LoggingService.Infrastructure.Messaging;
using LoggingService.Infrastructure.Persistence;
using LoggingService.Infrastructure.Services.ActivityLogs;
using LoggingService.Infrastructure.Services.Cleanup;
using LoggingService.Infrastructure.Services.ErrorLogs;
using LoggingService.Infrastructure.Services.LoginAudits;
using LoggingService.Infrastructure.Services.UserSyncs;
namespace LoggingService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LoggingDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(
                    typeof(LoggingDbContext).Assembly.FullName)));

        services.AddScoped<IErrorLogService, ErrorLogService>();
        services.AddScoped<IActivityLogService, ActivityLogService>();
        services.AddScoped<ILoginAuditService, LoginAuditService>();
        services.AddScoped<IUserSyncService, UserSyncService>();

        // RabbitMQ consumers for log events + UserSync
        services.AddHostedService<RabbitMqConsumerHostedService>();

        // Daily cleanup background service
        services.AddHostedService<LogCleanupService>();

        return services;
    }
}
