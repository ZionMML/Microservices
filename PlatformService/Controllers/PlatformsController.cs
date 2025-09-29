using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using PlatformService.AsyncDataServices;
using PlatformService.Data;
using PlatformService.Dtos;
using PlatformService.Models;
using PlatformService.SyncDataServices.Http;

namespace PlatformService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlatformsController(
    IPlatformRepo repository,
    IMapper mapper,
    ICommandDataClient commandDataClient,
    IMessageBusClient messageBusClient
) : ControllerBase
{
    private readonly IPlatformRepo _repository = repository;
    private readonly IMapper _mapper = mapper;
    private readonly ICommandDataClient _commandDataClient = commandDataClient;
    private IMessageBusClient _messageBusClient = messageBusClient;

    [HttpGet]
    public ActionResult<IEnumerable<PlatformReadDto>> GetPlatforms()
    {
        Console.WriteLine("-->Getting Platforms...");

        var platformItems = _repository.GetAllPlatforms();

        return Ok(_mapper.Map<IEnumerable<PlatformReadDto>>(platformItems));
    }

    [HttpGet("{id}", Name = "GetPlatformById")]
    public ActionResult<PlatformReadDto> GetPlatformById(int id)
    {
        var platformItem = _repository.GetPlatformById(id);
        if (platformItem != null)
        {
            return Ok(_mapper.Map<PlatformReadDto>(platformItem));
        }

        return NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<PlatformReadDto>> CreatePlatform(
        PlatformCreateDto platformCreateDto
    )
    {
        var platformModel = _mapper.Map<Platform>(platformCreateDto);
        _repository.CreatePlatform(platformModel);
        _repository.SaveChanges();

        var platformReadDto = _mapper.Map<PlatformReadDto>(platformModel);

        // Send Sync Message
        try
        {
            await _commandDataClient.SendPlatformToCommand(platformReadDto);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"--> Could not send synchronously:{ex.Message}");
        }

        //Send Async Message
        try
        {
            var platformpPublishedDto = _mapper.Map<PlatformPublishedDto>(platformReadDto);
            platformpPublishedDto.Event = "Platform_Published";
            _messageBusClient.PublishNewPlatform(platformpPublishedDto);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"--> Could not send asynchronously:{ex.Message}");
        }

        return CreatedAtRoute(nameof(GetPlatformById), new { platformReadDto.Id }, platformReadDto);
    }
}
