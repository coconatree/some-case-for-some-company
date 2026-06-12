using Autofac;
using Microsoft.AspNetCore.Mvc;

using MilkingSystem.Core.Services;
using MilkingSystem.Api.Requests;
using MilkingSystem.Api.Responses;

namespace MilkingSystem.Api.Controllers;

/// <summary>
/// Controller for weight measurements.
/// 
/// TODO: Candidates should implement POST endpoint for recording weight measurements.
/// 
/// Requirements:
/// - Accept weight data from robots (animalId, robotId, weightKg)
/// - Validate that animal and robot exist
/// - There is no "double-weighing" protection needed (unlike milking)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WeightsController : ControllerBase
{
    private readonly ILifetimeScope _scope;

    private readonly DataService _dataService;

    public WeightsController(ILifetimeScope scope, DataService dataService)
    {
        _scope = scope;
        _dataService = dataService;
    }

    [HttpGet("animal/{animalId}")]
    public IActionResult GetForAnimal(int animalId)
    {
        var measurements = _dataService.GetWeightMeasurementsForAnimal(animalId);
        return Ok(measurements);
    }

    [HttpGet("animal/{animalId}/last")]
    public IActionResult GetLastForAnimal(int animalId)
    {
        var lastMeasurement = _dataService.GetLastWeightForAnimal(animalId);

        if (lastMeasurement == null)
            return NotFound();

        return Ok(lastMeasurement);
    }

    [HttpPost]
    public IActionResult RecordWeight([FromBody] RecordWeightRequest request)
    {
        // TODO: Candidate should implement this endpoint
        // [HttpPost]
        // public IActionResult RecordWeight([FromBody] RecordWeightRequest request)
        // {
        //     // Implementation needed:
        //     // 1. Validate the request
        //     // 2. Check if animal exists
        //     // 3. Check if robot exists and is active
        //     // 4. Save the weight measurement
        // }

        try
        {
            ValidateRecordWeigthRequest(request);
        }
        catch (Exception exp)
        {
            return NotFound(exp.Message);
        }

        int id = _dataService.SaveWeightMeasurement(
            request.AnimalId,
            request.RobotId,
            request.Timestamp ?? DateTime.UtcNow,
            request.WeightKg
        );

        return Ok(new IdResponse(id));
    }

    private void ValidateRecordWeigthRequest(RecordWeightRequest request)
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

        // Is the weigth non-negative --- Done via annotation.
    }
}
