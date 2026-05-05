using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using LoggingService.Domain.Entities;
using LoggingService.Domain.Enums;
using LoggingService.Infrastructure.Persistence;
namespace LoggingService.Infrastructure.Messaging.Consumers;

public class LoginAuditConsumer : IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LoginAuditConsumer> _logger;
    private IChannel? _channel;

    public LoginAuditConsumer(IServiceScopeFactory scopeFactory, ILogger<LoginAuditConsumer> logger)
    { _scopeFactory = scopeFactory; _logger = logger; }

    public async Task StartAsync(IConnection connection, CancellationToken ct = default)
    {
        _channel = await connection.CreateChannelAsync(cancellationToken: ct);
        const string exchange = "logging.loginaudit";
        const string queue = "logging.loginaudit";
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
                var dto = JsonSerializer.Deserialize<LoginAuditEventDto>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true,
                        Converters = { new JsonStringEnumConverter() } });
                if (dto == null) return;

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<LoggingDbContext>();
                db.LoginAudits.Add(new LoginAudit
                {
                    Id = Guid.NewGuid(),
                    Timestamp = dto.Timestamp,
                    EventType = dto.EventType,
                    UserId = dto.UserId,
                    Email = dto.Email,
                    TenantId = dto.TenantId,
                    BranchId = dto.BranchId,
                    IpAddress = dto.IpAddress,
                    UserAgent = dto.UserAgent,
                    DeviceType = dto.DeviceType,
                    CreatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync(ct);
                await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoginAuditConsumer: failed");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true, ct);
            }
        };
        await _channel.BasicConsumeAsync(queue, autoAck: false, consumer, ct);
    }
    public void Dispose() => _channel?.Dispose();
}

public class LoginAuditEventDto
{
    public DateTime Timestamp { get; set; }
    public LoginEventType EventType { get; set; }
    public string? UserId { get; set; }
    public string Email { get; set; } = default!;
    public Guid? TenantId { get; set; }
    public Guid? BranchId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DeviceType DeviceType { get; set; } = DeviceType.Unknown;
}
