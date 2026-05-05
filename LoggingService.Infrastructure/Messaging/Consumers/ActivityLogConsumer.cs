using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using LoggingService.Domain.Entities;
using LoggingService.Infrastructure.Persistence;

namespace LoggingService.Infrastructure.Messaging.Consumers;

public class ActivityLogConsumer : IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ActivityLogConsumer> _logger;
    private IChannel? _channel;

    public ActivityLogConsumer(IServiceScopeFactory scopeFactory, ILogger<ActivityLogConsumer> logger)
    { _scopeFactory = scopeFactory; _logger = logger; }

    public async Task StartAsync(IConnection connection, CancellationToken ct = default)
    {
        _channel = await connection.CreateChannelAsync(cancellationToken: ct);
        const string exchange = "logging.activity";
        const string queue = "logging.activitylog";
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
                var dto = JsonSerializer.Deserialize<ActivityLogEventDto>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true,
                        Converters = { new JsonStringEnumConverter(), new NumberOrStringConverter() } });
                if (dto == null) return;

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<LoggingDbContext>();
                db.ActivityLogs.Add(new ActivityLog
                {
                    Id = Guid.NewGuid(),
                    Timestamp = dto.Timestamp,
                    ActionType = dto.ActionType,
                    EntityType = dto.EntityType,
                    EntityId = dto.EntityId,
                    ServiceName = dto.ServiceName,
                    Description = dto.Description,
                    OldValues = dto.OldValues,
                    NewValues = dto.NewValues,
                    UserId = dto.UserId,
                    UserEmail = dto.UserEmail,
                    UserRole = dto.UserRole,
                    TenantId = dto.TenantId,
                    BranchId = dto.BranchId,
                    IpAddress = dto.IpAddress,
                    CorrelationId = dto.CorrelationId,
                    CreatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync(ct);
                await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ActivityLogConsumer: failed");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true, ct);
            }
        };
        await _channel.BasicConsumeAsync(queue, autoAck: false, consumer, ct);
    }
    public void Dispose() => _channel?.Dispose();
}

public class ActivityLogEventDto
{
    public DateTime Timestamp { get; set; }
    public string ActionType { get; set; } = default!;
    public string EntityType { get; set; } = default!;
    public string? EntityId { get; set; }
    public string ServiceName { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserRole { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? BranchId { get; set; }
    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; }
}

/// <summary>
/// Handles fields declared as string in the DTO but published as a JSON number by other services.
/// Accepts both token types — numbers are converted to their string representation.
/// </summary>
public class NumberOrStringConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.TryGetInt64(out var l) ? l.ToString() : reader.GetDouble().ToString(),
            JsonTokenType.True   => "true",
            JsonTokenType.False  => "false",
            JsonTokenType.Null   => null,
            _ => throw new JsonException($"Cannot convert token type '{reader.TokenType}' to string.")
        };
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else writer.WriteStringValue(value);
    }
}
