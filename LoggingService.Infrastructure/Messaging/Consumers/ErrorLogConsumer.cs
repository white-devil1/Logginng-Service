using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using LoggingService.Domain.Entities;
using LoggingService.Domain.Enums;
using LoggingService.Infrastructure.Persistence;
namespace LoggingService.Infrastructure.Messaging.Consumers;

public class ErrorLogConsumer : IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ErrorLogConsumer> _logger;
    private IChannel? _channel;

    public ErrorLogConsumer(IServiceScopeFactory scopeFactory, ILogger<ErrorLogConsumer> logger)
    { _scopeFactory = scopeFactory; _logger = logger; }

    public async Task StartAsync(IConnection connection, CancellationToken ct = default)
    {
        _channel = await connection.CreateChannelAsync(cancellationToken: ct);
        const string exchange = "logging.error";
        const string queue = "logging.errorlog";
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
                var dto = JsonSerializer.Deserialize<ErrorLogEventDto>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (dto == null) return;

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<LoggingDbContext>();
                db.ErrorLogs.Add(new ErrorLog
                {
                    Id = Guid.NewGuid(),
                    Timestamp = dto.Timestamp,
                    Severity = dto.Severity,
                    Source = dto.Source,
                    Category = dto.Category,
                    ServiceName = dto.ServiceName,
                    Environment = dto.Environment ?? "Development",
                    Message = dto.Message,
                    StackTrace = dto.StackTrace,
                    RequestPath = dto.RequestPath,
                    RequestMethod = dto.RequestMethod,
                    StatusCode = dto.StatusCode,
                    UserId = dto.UserId,
                    UserEmail = dto.UserEmail,
                    TenantId = dto.TenantId,
                    BranchId = dto.BranchId,
                    IpAddress = dto.IpAddress,
                    UserAgent = dto.UserAgent,
                    AdditionalData = dto.AdditionalData,
                    CreatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync(ct);
                await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ErrorLogConsumer: failed");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true, ct);
            }
        };
        await _channel.BasicConsumeAsync(queue, autoAck: false, consumer, ct);
    }
    public void Dispose() => _channel?.Dispose();
}

// DTO matches what GlobalExceptionHandlingMiddleware publishes
public class ErrorLogEventDto
{
    public DateTime Timestamp { get; set; }
    public Severity Severity { get; set; } = Severity.Error;
    public LogSource Source { get; set; } = LogSource.Backend;
    public ErrorCategory Category { get; set; } = ErrorCategory.ServerError;
    public string ServiceName { get; set; } = default!;
    public string? Environment { get; set; }
    public string Message { get; set; } = default!;
    public string? StackTrace { get; set; }
    public string? RequestPath { get; set; }
    public string? RequestMethod { get; set; }
    public int? StatusCode { get; set; }
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? BranchId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? AdditionalData { get; set; }
}
