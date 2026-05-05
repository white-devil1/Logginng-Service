using LoggingService.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LoggingService.Infrastructure.Messaging.Consumers;

public class UserStatusChangedConsumer : IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserStatusChangedConsumer> _logger;
    private IChannel? _channel;

    public UserStatusChangedConsumer(IServiceScopeFactory scopeFactory,
        ILogger<UserStatusChangedConsumer> logger)
    { _scopeFactory = scopeFactory; _logger = logger; }

    public async Task StartAsync(IConnection connection, CancellationToken ct = default)
    {
        _channel = await connection.CreateChannelAsync(cancellationToken: ct);
        var exchange = "UserManagementService.Application.Events.UserStatusChangedEvent";
        var queue = "companysettings.user.statuschanged";
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
                var evt = JsonSerializer.Deserialize<UserStatusChangedDto>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true,
                        Converters = { new JsonStringEnumConverter() } });
                if (evt == null) return;
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IUserSyncService>();
                var user = await svc.GetByUserIdAsync(evt.UserId, ct);
                if (user != null)
                {
                    user.IsActive = evt.IsActive;
                    user.LastSyncedAt = DateTime.UtcNow;
                    user.UpdatedAt = DateTime.UtcNow;
                    await svc.UpsertAsync(user, ct);
                }
                await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                _logger.LogInformation(
                    "UserSync: status changed {UserId} IsActive={IsActive}",
                    evt.UserId, evt.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserStatusChangedConsumer failed");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true, ct);
            }
        };
        await _channel.BasicConsumeAsync(queue, autoAck: false, consumer, ct);
    }
    public void Dispose() => _channel?.Dispose();
}

public class UserStatusChangedDto
{
    public string UserId { get; set; } = default!;
    public bool IsActive { get; set; }
    public Guid TenantId { get; set; }
}
