using LoggingService.Application.Services;
using LoggingService.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LoggingService.Infrastructure.Messaging.Consumers;

public class UserUpdatedConsumer : IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserUpdatedConsumer> _logger;
    private IChannel? _channel;

    public UserUpdatedConsumer(IServiceScopeFactory scopeFactory,
        ILogger<UserUpdatedConsumer> logger)
    { _scopeFactory = scopeFactory; _logger = logger; }

    public async Task StartAsync(IConnection connection, CancellationToken ct = default)
    {
        _channel = await connection.CreateChannelAsync(cancellationToken: ct);
        var exchange = "UserManagementService.Application.Events.UserUpdatedEvent";
        var queue = "companysettings.user.updated";
        await _channel.ExchangeDeclareAsync(exchange, ExchangeType.Fanout,
            durable: true, autoDelete: false, cancellationToken: ct);
        await _channel.QueueDeclareAsync(queue, durable: true,
            exclusive: false, autoDelete: false, cancellationToken: ct);
        await _channel.QueueBindAsync(queue, exchange, "", cancellationToken: ct);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var evt = JsonSerializer.Deserialize<UserUpdatedEventDto>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true,
                        Converters = { new JsonStringEnumConverter() } });
                if (evt == null) return;
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IUserSyncService>();
                await svc.UpsertAsync(new UserSync
                {
                    UserId = evt.UserId,
                    Email = evt.Email,
                    UserName = evt.UserName,
                    FirstName = evt.FirstName,
                    LastName = evt.LastName,
                    TenantId = evt.TenantId,
                    BranchId = evt.BranchId,
                    IsActive = evt.IsActive,
                    IsDeleted = evt.IsDeleted,
                    IsSuperAdmin = false,
                    Roles = JsonSerializer.Serialize(evt.Roles),
                    LastSyncedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }, ct);
                await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                _logger.LogInformation("UserSync: updated user {UserId}", evt.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserUpdatedConsumer failed");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true, ct);
            }
        };
        await _channel.BasicConsumeAsync(queue, autoAck: false, consumer, ct);
    }
    public void Dispose() => _channel?.Dispose();
}

public class UserUpdatedEventDto
{
    public string UserId { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string UserName { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public Guid TenantId { get; set; }
    public Guid? BranchId { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public List<string> Roles { get; set; } = new();
}
