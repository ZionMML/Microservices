using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Metadata;
using PlatformService.Dtos;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace PlatformService.AsyncDataServices;

public class MessageBusClient : IMessageBusClient
{
    private readonly IConfiguration _configuration;
    private readonly IConnection? _connection;
    private readonly IChannel? _channel;
    private const string ExchangeName = "trigger_exchange";

    public MessageBusClient(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        var factory = new ConnectionFactory
        {
            HostName =
                _configuration["RabbitMQHost"]
                ?? throw new ArgumentNullException(nameof(configuration)),
            Port = int.Parse(
                _configuration["RabbitMQPort"]
                    ?? throw new ArgumentNullException(nameof(configuration))
            ),
        };

        try
        {
            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            _connection.ConnectionShutdownAsync += OnConnectionShutdown;

            _channel
                .ExchangeDeclareAsync(exchange: ExchangeName, type: ExchangeType.Fanout)
                .GetAwaiter()
                .GetResult();

            Console.WriteLine("--> Connected to Message Bus");
        }
        catch (BrokerUnreachableException ex)
        {
            Console.WriteLine($"--> Could not connect to the Message Bus: {ex.Message}");
            _connection = null;
            _channel = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"--> An unexpected error occurred during Message Bus setup: {ex.Message}"
            );
            _connection = null;
            _channel = null;
        }
    }

    private Task OnConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
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

    public void PublishNewPlatform(PlatformPublishedDto platformPublishedDto)
    {
        var message = JsonSerializer.Serialize(platformPublishedDto);

        if (_connection != null && _connection.IsOpen)
        {
            Console.WriteLine("--> RabbitMQ Connection Open, sending messsage...");
            SendMessage(message);
        }
        else
        {
            Console.WriteLine("--> RabbitMQ connection is closed, not sending");
        }
    }

    private void SendMessage(string message)
    {
        if (_channel == null)
        {
            Console.WriteLine("--> Error: Channel is null, cannot send message.");
            return;
        }

        var body = Encoding.UTF8.GetBytes(message);

        var properties = new RabbitMQ.Client.BasicProperties();

        try
        {
            _channel
                .BasicPublishAsync(
                    exchange: ExchangeName,
                    routingKey: "",
                    mandatory: false,
                    basicProperties: properties,
                    body: body,
                    cancellationToken: CancellationToken.None
                )
                .GetAwaiter()
                .GetResult();

            Console.WriteLine($"--> We have sent {message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"--> An unexpected error occurred during sending message: {ex.Message}"
            );
        }
    }

    public void Dispose()
    {
        Console.WriteLine("MessageBus Disposed");
        if (_channel != null && _channel.IsOpen && _connection != null)
        {
            _channel.CloseAsync();
            _connection.CloseAsync();
        }
    }
}
