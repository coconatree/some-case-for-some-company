using Autofac;
using Microsoft.AspNetCore.Mvc;
using MilkingSystem.Core.Services;

namespace MilkingSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RobotsController : ControllerBase
{
    private readonly ILifetimeScope _scope;

    private readonly DataService _dataService;

    public RobotsController(ILifetimeScope scope, DataService dataService)
    {
        _scope = scope;
        _dataService = dataService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var robots = _dataService.GetAllRobots();
        return Ok(robots);
    }

    [HttpGet("{id}")]
    public IActionResult Get(int id)
    {
        var robot = _dataService.GetRobotById(id);
        
        if (robot == null)
            return NotFound();
        
        return Ok(robot);
    }

    [HttpGet("active")]
    public IActionResult GetActive()
    {
        var robots = _dataService.GetActiveRobots();
        return Ok(robots);
    }
}
