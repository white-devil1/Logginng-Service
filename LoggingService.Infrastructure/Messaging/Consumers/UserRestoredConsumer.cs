using LoggingService.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LoggingService.Infrastructure.Messaging.Consumers;

public class UserRestoredConsumer : IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserRestoredConsumer> _logger;
    private IChannel? _channel;

    public UserRestoredConsumer(IServiceScopeFactory scopeFactory,
        ILogger<UserRestoredConsumer> logger)
    { _scopeFactory = scopeFactory; _logger = logger; }

    public async Task StartAsync(IConnection connection, CancellationToken ct = default)
    {
        _channel = await connection.CreateChannelAsync(cancellationToken: ct);
        var exchange = "UserManagementService.Application.Events.UserRestoredEvent";
        var queue = "companysettings.user.restored";
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
                var evt = JsonSerializer.Deserialize<UserRestoredDto>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true,
                        Converters = { new JsonStringEnumConverter() } });
                if (evt == null) return;
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IUserSyncService>();
                await svc.MarkRestoredAsync(evt.UserId, ct);
                await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                _logger.LogInformation("UserSync: restored user {UserId}", evt.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRestoredConsumer failed");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true, ct);
            }
        };
        await _channel.BasicConsumeAsync(queue, autoAck: false, consumer, ct);
    }
    public void Dispose() => _channel?.Dispose();
}

public class UserRestoredDto { public string UserId { get; set; } = default!; }
