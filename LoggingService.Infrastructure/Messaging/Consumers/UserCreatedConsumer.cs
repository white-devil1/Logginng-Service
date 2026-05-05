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

public class UserCreatedConsumer : IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserCreatedConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public UserCreatedConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<UserCreatedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(
        IConnection connection, CancellationToken ct = default)
    {
        _connection = connection;
        _channel = await connection.CreateChannelAsync(cancellationToken: ct);

        var exchange = "UserManagementService.Application.Events.UserCreatedEvent";
        var queue = "companysettings.user.created";

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
                var evt = JsonSerializer.Deserialize<UserCreatedEventDto>(json,
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
                    IsDeleted = false,
                    IsSuperAdmin = evt.IsSuperAdmin,
                    Roles = JsonSerializer.Serialize(evt.Roles),
                    LastSyncedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }, ct);

                await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                _logger.LogInformation(
                    "UserSync: created user {UserId}", evt.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserCreatedConsumer: failed to process message");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true, ct);
            }
        };

        await _channel.BasicConsumeAsync(queue, autoAck: false, consumer, ct);
        _logger.LogInformation("UserCreatedConsumer: listening on {Queue}", queue);
    }

    public void Dispose()
    {
        _channel?.Dispose();
    }
}

// Local DTO matching UserManagementService.Application.Events.UserCreatedEvent
public class UserCreatedEventDto
{
    public string UserId { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string UserName { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public Guid TenantId { get; set; }
    public Guid? BranchId { get; set; }
    public bool IsSuperAdmin { get; set; }
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
}
