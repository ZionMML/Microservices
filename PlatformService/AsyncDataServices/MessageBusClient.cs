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
    private IConnection? _connection;
    private IChannel? _channel;
    private const string ExchangeName = "trigger_exchange";

    public MessageBusClient(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task InitializeAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName =
                _configuration["RabbitMQHost"]
                ?? throw new ArgumentNullException(nameof(_configuration)),
            Port = int.Parse(
                _configuration["RabbitMQPort"]
                    ?? throw new ArgumentNullException(nameof(_configuration))
            ),
        };

        try
        {
            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.ExchangeDeclareAsync(exchange: ExchangeName, type: ExchangeType.Fanout);

            _connection.ConnectionShutdownAsync += OnConnectionShutdown;

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

    public async Task PublishNewPlatformAsync(PlatformPublishedDto platformPublishedDto)
    {
        if (_connection == null || !_connection.IsOpen || _channel == null)
        {
            Console.WriteLine("--> RabbitMQ connection not open, initializing...");
            await InitializeAsync();
        }

        if (_connection == null || !_connection.IsOpen || _channel == null)
        {
            Console.WriteLine("--> RabbitMQ connection is closed, not sending");
            return;
        }

        var message = JsonSerializer.Serialize(platformPublishedDto);
        var body = Encoding.UTF8.GetBytes(message);
        var properties = new RabbitMQ.Client.BasicProperties();

        try
        {
            await _channel.BasicPublishAsync(
                exchange: ExchangeName,
                routingKey: "",
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: CancellationToken.None
            );

            Console.WriteLine($"--> We have sent {message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"--> An unexpected error occurred during sending message: {ex.Message}"
            );
        }
    }

    public async ValueTask DisposeAsync()
    {
        Console.WriteLine("MessageBus Disposed");
        if (_channel != null)
            await _channel.CloseAsync();

        if (_connection != null)
            await _connection.CloseAsync();
    }
}
