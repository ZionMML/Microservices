using System.ComponentModel.Design;
using AutoMapper;
using CommandService.Data;
using CommandService.Dtos;
using CommandService.Models;
using Microsoft.AspNetCore.Mvc;

namespace CommandService.Controllers;

[Route("api/c/platforms/{platformId}/[controller]")]
[ApiController]
public class CommandsController(ICommandRepo repo, IMapper mapper) : ControllerBase
{
    private readonly ICommandRepo _repo = repo;
    private readonly IMapper _mapper = mapper;

    [HttpGet]
    public ActionResult<IEnumerable<CommandReadDto>> GetCommandsForPlatform(int platformId)
    {
        Console.WriteLine($"--> Hit GetCommandsForPlatform: {platformId}");

        if (!_repo.PlatformExists(platformId))
        {
            return NotFound();
        }

        var commands = _repo.GetCommandsForPlatform(platformId);

        return Ok(_mapper.Map<IEnumerable<CommandReadDto>>(commands));
    }

    [HttpGet("{commandId}", Name = "GetCommandForPlatform")]
    public ActionResult<CommandReadDto> GetCommandForPlatform(int platformId, int commandId)
    {
        Console.WriteLine($"--> Hit GetCommandForPlatform: {platformId}/{commandId}");

        if (!_repo.PlatformExists(platformId))
        {
            return NotFound();
        }

        var command = _repo.GetCommand(platformId, commandId);

        if (command == null)
        {
            return NotFound();
        }

        return Ok(_mapper.Map<CommandReadDto>(command));
    }

    [HttpPost]
    public ActionResult<CommandReadDto> CreateCommandForPlatform(
        int platformId,
        CommandCreateDto commandDto
    )
    {
        Console.WriteLine($"--> Hit CreateCommandForPlatform: {platformId}");

        if (!_repo.PlatformExists(platformId))
        {
            return NotFound();
        }

        var command = _mapper.Map<Command>(commandDto);

        _repo.CreateCommand(platformId, command);
        _repo.SaveChanges();

        var commandReadDto = _mapper.Map<CommandReadDto>(command);

        return CreatedAtRoute(
            nameof(GetCommandForPlatform),
            new { platformId = platformId, commandId = commandReadDto.Id },
            commandReadDto
        );
    }
}
