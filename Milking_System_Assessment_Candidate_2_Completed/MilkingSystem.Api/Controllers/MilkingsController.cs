using Autofac;
using Microsoft.AspNetCore.Mvc;

using MilkingSystem.Core.Models;
using MilkingSystem.Core.Services;
using MilkingSystem.Core.Notifications;
using MilkingSystem.Api.Requests;

namespace MilkingSystem.Api.Controllers;

/// <summary>
/// Controller for milking events.
/// 
/// TODO: Candidates should implement POST endpoint for recording new milking events.
/// 
/// Background:
/// - Robots are large stationary machines in the barn
/// - A cow walks into a robot and gets milked autonomously
/// - After milking, a cow might walk to another robot hoping for more food
/// - Other robots must know NOT to milk this cow (she was recently milked)
/// 
/// Requirements:
/// - Accept milking data from robots (animalId, robotId, milkYieldLiters, duration)
/// - Prevent double-milking: if an animal was milked within the last 6 hours, reject the request
/// - Notify other robots about the milking event using IRobotNotifier
/// - Handle concurrent messages from multiple robots about different cows
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MilkingsController : ControllerBase
{
    private readonly ILifetimeScope _scope;
    private readonly DataService _dataService;

    private readonly IRobotNotifier _notificationService;

    public MilkingsController(ILifetimeScope scope, DataService dataService, IRobotNotifier notificationService)
    {
        _scope = scope;
        _dataService = dataService;
        _notificationService = notificationService;
    }

    [HttpGet("animal/{animalId}")]
    public IActionResult GetForAnimal(int animalId)
    {
        var events = _dataService.GetMilkingEventsForAnimal(animalId);
        return Ok(events);
    }

    [HttpGet("animal/{animalId}/last")]
    public IActionResult GetLastForAnimal(int animalId)
    {
        var lastEvent = _dataService.GetLastMilkingForAnimal(animalId);

        if (lastEvent == null)
            return NotFound();

        return Ok(lastEvent);
    }

    [HttpGet("recent")]
    public IActionResult GetRecent([FromQuery] int hours = 24)
    {
        var events = _dataService.GetRecentMilkingEvents(hours);
        return Ok(events);
    }

    [HttpPost]
    public IActionResult RecordMilking([FromBody] RecordMilkingRequest request)
    {
        // Implementation needed:
        // 1. Validate the request
        // 2. Check if animal exists
        // 3. Check if robot exists and is active
        // 4. Check if animal was recently milked (within 6 hours) - use IRobotNotifier.WasRecentlyMilked
        // 5. Save the milking event
        // 6. Notify other robots using IRobotNotifier.NotifyMilkingCompleted
        // 7. Handle concurrency (what if two robots try to milk same animal at same time?)

        // Validate
        try
        {
            ValidateRecordMilkingRequest(request);
        }
        catch (Exception exp)
        {
            return NotFound(exp.Message);
        }

        var result = _dataService.ProcessMilkingRequest(
            request.AnimalId,
            request.RobotId,
            request.Timestamp ?? DateTime.UtcNow,
            request.MilkYieldLiters,
            request.Duration,
            _notificationService // I don't like this, but I am out of time!
        );

        if (!result.Success)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok("Milking successfully recorded!");
    }

    private void ValidateRecordMilkingRequest(RecordMilkingRequest request)
    {
        // Does animal exists

        if (!_dataService.DoesAnimalExistsById(request.AnimalId))
        {
            throw new Exception(
                $"Animal with id: {request.AnimalId} does not exist."
            );
        }

        // Does robot exists

        if (!_dataService.DoesActiveRobotExistsById(request.RobotId))
        {
            throw new Exception(
                $"Active robot with id: {request.RobotId} does not exist."
            );
        }

        // Is the milk yield non-negative --- Done via annotation.
    }
}


