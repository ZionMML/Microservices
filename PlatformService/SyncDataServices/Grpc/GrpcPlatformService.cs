using AutoMapper;
using Grpc.Core;
using PlatformService.Data;

namespace PlatformService.SyncDataServices.Grpc;

public class GrpcPlatformService(IPlatformRepo repository, IMapper mapper)
    : GrpcPlatform.GrpcPlatformBase
{
    private readonly IPlatformRepo _repository = repository;
    private readonly IMapper _mapper = mapper;

    public override Task<PlatformResponse> GetAllPlatforms(
        GetAllRequests request,
        ServerCallContext context
    )
    {
        var respone = new PlatformResponse();
        var platforms = _repository.GetAllPlatforms();

        foreach (var plat in platforms)
        {
            respone.Platform.Add(_mapper.Map<GrpcPlatformModel>(plat));
        }

        return Task.FromResult(respone);
    }
}
