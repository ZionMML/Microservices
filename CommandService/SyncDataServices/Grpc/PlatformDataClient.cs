using AutoMapper;
using CommandService.Models;
using Grpc.Core;
using Grpc.Net.Client;
using PlatformService;
using RabbitMQ.Client;

namespace CommandService.SyncDataServices.Grpc;

public class PlatformDataClient(IConfiguration configuration, IMapper mapper) : IPlatformDataClient
{
    private readonly IConfiguration _configuration = configuration;
    private readonly IMapper _mapper = mapper;

    public IEnumerable<Platform>? ReturnAllPlatforms()
    {
        Console.WriteLine($"--> Calling GRPC Service {_configuration["GrpcPlatform"]}");

        var channel = GrpcChannel.ForAddress(
            _configuration["GrpcPlatform"]
                ?? throw new Exception("No GrpcPlatform configuration found")
        );
        var client = new GrpcPlatform.GrpcPlatformClient(channel);
        var request = new GetAllRequests();

        try
        {
            var reply = client.GetAllPlatforms(request);
            return _mapper.Map<IEnumerable<Platform>>(reply.Platform);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"--> Could not call GRPC Server {ex.Message}");
            return null;
        }
    }
}
