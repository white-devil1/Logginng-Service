using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using LoggingService.Infrastructure.Messaging.Consumers;
namespace LoggingService.Infrastructure.Messaging;

public class RabbitMqConsumerHostedService : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _cfg;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<RabbitMqConsumerHostedService> _logger;
    private IConnection? _connection;
    private readonly List<IDisposable> _consumers = new();

    public RabbitMqConsumerHostedService(IServiceScopeFactory scopeFactory,
        IConfiguration cfg, ILoggerFactory loggerFactory)
    {
        _scopeFactory = scopeFactory; _cfg = cfg; _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<RabbitMqConsumerHostedService>();
    }

    public async Task StartAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = _cfg["RabbitMQ:Host"] ?? "localhost",
            UserName = _cfg["RabbitMQ:Username"] ?? "guest",
            Password = _cfg["RabbitMQ:Password"] ?? "guest",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };
        _connection = await factory.CreateConnectionAsync(ct);
        _logger.LogInformation("Logging RabbitMQ: connected to {H}", factory.HostName);

        // Log-type consumers
        var c1 = new ErrorLogConsumer(_scopeFactory, _loggerFactory.CreateLogger<ErrorLogConsumer>());
        await c1.StartAsync(_connection, ct); _consumers.Add(c1);

        var c2 = new ActivityLogConsumer(_scopeFactory, _loggerFactory.CreateLogger<ActivityLogConsumer>());
        await c2.StartAsync(_connection, ct); _consumers.Add(c2);

        var c3 = new LoginAuditConsumer(_scopeFactory, _loggerFactory.CreateLogger<LoginAuditConsumer>());
        await c3.StartAsync(_connection, ct); _consumers.Add(c3);

        // UserSync consumers
        var c4 = new UserCreatedConsumer(_scopeFactory, _loggerFactory.CreateLogger<UserCreatedConsumer>());
        await c4.StartAsync(_connection, ct); _consumers.Add(c4);

        var c5 = new UserUpdatedConsumer(_scopeFactory, _loggerFactory.CreateLogger<UserUpdatedConsumer>());
        await c5.StartAsync(_connection, ct); _consumers.Add(c5);

        var c6 = new UserStatusChangedConsumer(_scopeFactory, _loggerFactory.CreateLogger<UserStatusChangedConsumer>());
        await c6.StartAsync(_connection, ct); _consumers.Add(c6);

        var c7 = new UserDeletedConsumer(_scopeFactory, _loggerFactory.CreateLogger<UserDeletedConsumer>());
        await c7.StartAsync(_connection, ct); _consumers.Add(c7);

        var c8 = new UserRestoredConsumer(_scopeFactory, _loggerFactory.CreateLogger<UserRestoredConsumer>());
        await c8.StartAsync(_connection, ct); _consumers.Add(c8);

        _logger.LogInformation("Logging RabbitMQ: all 8 consumers started");
    }

    public Task StopAsync(CancellationToken ct)
    { _logger.LogInformation("Logging RabbitMQ: stopping"); return Task.CompletedTask; }

    public void Dispose()
    { foreach (var c in _consumers) c.Dispose(); _connection?.Dispose(); }
}
