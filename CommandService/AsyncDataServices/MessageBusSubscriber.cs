using System.Text;
using CommandService.EventProcessing;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CommandService.AsyncDataServices;

public class MessageBusSubscriber : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IEventProcessor _eventprocesser;

    private IConnection? _connection;
    private IChannel? _channel;

    private string? _queueName;

    public MessageBusSubscriber(IConfiguration configuration, IEventProcessor eventProcessor)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _eventprocesser = eventProcessor ?? throw new ArgumentNullException(nameof(eventProcessor));
    }

    private async Task InitializeRabbitMQ()
    {
        var factory = new ConnectionFactory()
        {
            HostName =
                _configuration["RabbitMQHost"]
                ?? throw new ArgumentNullException(nameof(_configuration)),
            Port = int.Parse(
                _configuration["RabbitMQPort"]
                    ?? throw new ArgumentNullException(nameof(_configuration))
            ),
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.ExchangeDeclareAsync(
            exchange: "trigger_exchange",
            type: ExchangeType.Fanout
        );

        var queueDeclareOk = await _channel.QueueDeclareAsync();

        _queueName = queueDeclareOk.QueueName;
        await _channel.QueueBindAsync(
            queue: _queueName,
            exchange: "trigger_exchange",
            routingKey: ""
        );

        Console.WriteLine("--> Listenging on the Message Bus...");
        _connection.ConnectionShutdownAsync += RabbitMQ_ConnectionShutdown;
    }

    private Task RabbitMQ_ConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        Console.WriteLine("--> Connection Shutdown");

        if (e.Initiator != ShutdownInitiator.Application)
        {
            // The broker or network initiated the shutdown
            Console.WriteLine(
                $"--> RabbitMQ Connection SHUTDOWN unexpectedly. Reason: {e.Cause}, ClassId: {e.ClassId}, MethodId: {e.MethodId}"
            );
        }
        else
        {
            Console.WriteLine("--> RabbitMQ Connection SHUTDOWN by application.");
        }

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        if (_channel != null && _channel.IsOpen && _connection != null)
        {
            _channel.CloseAsync();
            _connection.CloseAsync();
        }

        base.Dispose();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeRabbitMQ();

        stoppingToken.ThrowIfCancellationRequested();

        var consumer = new AsyncEventingBasicConsumer(
            _channel ?? throw new ArgumentNullException("_channel is not initialized")
        );

        consumer.ReceivedAsync += (ModuleHandle, ea) =>
        {
            Console.WriteLine("--> Event Received");

            var body = ea.Body;
            var notificationMessage = Encoding.UTF8.GetString(body.ToArray());

            _eventprocesser.ProcessEvent(notificationMessage);

            return Task.CompletedTask;
        };

        await _channel.BasicConsumeAsync(
            queue: _queueName ?? throw new ArgumentNullException("_queueName is not initialized"),
            autoAck: true,
            consumer: consumer,
            cancellationToken: stoppingToken
        );
    }
}
