using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

using MilkingSystem.Api.Controllers;
using MilkingSystem.Api.Requests;
using MilkingSystem.Api.Responses;

using MilkingSystem.Core.Models;

namespace MilkingSystem.Tests.Controllers;

public class WeigthsControllerTests :
    IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly string CREATE_ANIMAL_PATH = "/api/animals";

    private static readonly string GET_ACTIVE_ROBOT_PATH = "/api/robots/active";

    private static readonly string RECORD_WEIGTHS_PATH = "/api/weights";

    private static readonly string GET_WEIGTH_RECORDING_FOR_ANIMAL_PATH = "/api/weights/animal/{animalId}";

    // Ideally, the repository methods that perform existence checks need to be mocked.
    private static readonly int NON_EXISTENT_ID = int.MaxValue;

    private readonly HttpClient _client;

    public WeigthsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task When_New_Weigth_Record_Created_Then_200()
    {
        // Create animal

        var createAnimalRequest = new CreateAnimalRequest
        {
            IdentificationNumber = Guid.NewGuid().ToString(),
        };

        var createAnimalResponse = await _client.PostAsJsonAsync(
            CREATE_ANIMAL_PATH,
            createAnimalRequest
        );

        createAnimalResponse.EnsureSuccessStatusCode();

        var createdAnimalIdResponse = await createAnimalResponse.Content.ReadFromJsonAsync<IdResponse>();

        // Get active robot
        // Ideally would create a new robot. Can cause flaky tests.

        var getActiveRobotResponse = await _client.GetAsync(
            GET_ACTIVE_ROBOT_PATH
        );

        getActiveRobotResponse.EnsureSuccessStatusCode();

        var activeRobotList = await getActiveRobotResponse.Content
            .ReadFromJsonAsync<List<Robot>>();

        Assert.NotNull(activeRobotList);
        Assert.NotEmpty(activeRobotList);
        Assert.NotNull(activeRobotList[0]);

        // Send weigth measurment request
        var request = new RecordWeightRequest
        {
            AnimalId = createdAnimalIdResponse.Id,
            RobotId = activeRobotList[0].Id,
            WeightKg = 500.11m,
            Timestamp = DateTime.UtcNow
        };

        var response = await _client.PostAsJsonAsync(
            RECORD_WEIGTHS_PATH,
            request
        );

        response.EnsureSuccessStatusCode();

        var createdWeigthRecordIdResponse = await response.Content.ReadFromJsonAsync<IdResponse>();

        Assert.NotNull(createdWeigthRecordIdResponse);

        // Validate

        // We need to fetch all and check if it exists as validating the 
        // latest could cause flaky tests. 

        var animalWeigthRecordListResponse = await _client.GetAsync(
            GET_WEIGTH_RECORDING_FOR_ANIMAL_PATH.Replace(
                "{animalId}",
                createdAnimalIdResponse.Id.ToString()
            )
        );

        var animalWeigthRecordList = await animalWeigthRecordListResponse.Content
            .ReadFromJsonAsync<List<WeightMeasurement>>();

        Assert.NotNull(animalWeigthRecordList);
        Assert.NotEmpty(animalWeigthRecordList);

        // Valdiate it contains the newly created record

        var createdRecord = animalWeigthRecordList
            .SingleOrDefault(x => x.Id == createdWeigthRecordIdResponse.Id);

        Assert.NotNull(createdRecord);

        Assert.Equal(request.AnimalId, createdRecord.AnimalId);
        Assert.Equal(request.RobotId, createdRecord.RobotId);
        Assert.Equal(request.WeightKg, createdRecord.WeightKg);

        // Assert.Equal(request.Timestamp, createdRecord.Timestamp);

        Assert.NotEqual(default(DateTime), createdRecord.Timestamp);
    }

    [Fact]
    public async Task When_Animal_Does_Not_Exist_Then_404()
    {
        // Fetch active robot

        var activeRobotResponse = await _client.GetAsync(
            GET_ACTIVE_ROBOT_PATH
        );

        activeRobotResponse.EnsureSuccessStatusCode();

        var robots = await activeRobotResponse.Content
            .ReadFromJsonAsync<List<Robot>>();

        Assert.NotNull(robots);
        Assert.NotEmpty(robots);
        Assert.NotNull(robots[0]);

        var request = new RecordWeightRequest
        {
            AnimalId = NON_EXISTENT_ID,
            RobotId = robots[0].Id,
            WeightKg = 500,
            Timestamp = DateTime.UtcNow
        };

        var response = await _client.PostAsJsonAsync(
        RECORD_WEIGTHS_PATH,
        request
    );

        Assert.Equal(
        System.Net.HttpStatusCode.NotFound,
        response.StatusCode
    );

        var error = await response.Content.ReadAsStringAsync();

        Assert.Contains("Animal", error);
        Assert.Contains("does not exist", error);
    }

    [Fact]
    public async Task When_Robot_Does_Not_Exist_Then_404()
    {
        // Create animal

        var createAnimalResponse = await _client.PostAsJsonAsync(
            CREATE_ANIMAL_PATH,
            new CreateAnimalRequest
            {
                IdentificationNumber = Guid.NewGuid().ToString()
            }
        );

        createAnimalResponse.EnsureSuccessStatusCode();

        var animal = await createAnimalResponse.Content
            .ReadFromJsonAsync<IdResponse>();

        Assert.NotNull(animal);

        var request = new RecordWeightRequest
        {
            AnimalId = animal.Id,
            RobotId = NON_EXISTENT_ID,
            WeightKg = 500,
            Timestamp = DateTime.UtcNow
        };

        var response = await _client.PostAsJsonAsync(
            RECORD_WEIGTHS_PATH,
            request
        );

        Assert.Equal(
            System.Net.HttpStatusCode.NotFound,
            response.StatusCode
        );

        var error = await response.Content.ReadAsStringAsync();

        Assert.Contains("Active robot", error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not exist", error);
    }

    [Fact]
    public async Task When_Weight_Is_Negative_Then_400()
    {
        // Create animal

        var createAnimalResponse = await _client.PostAsJsonAsync(
            CREATE_ANIMAL_PATH,
            new CreateAnimalRequest
            {
                IdentificationNumber = Guid.NewGuid().ToString()
            }
        );

        createAnimalResponse.EnsureSuccessStatusCode();

        var animal = await createAnimalResponse.Content
            .ReadFromJsonAsync<IdResponse>();

        Assert.NotNull(animal);

        // Fetch active robot

        var activeRobotResponse = await _client.GetAsync(
            GET_ACTIVE_ROBOT_PATH
        );

        activeRobotResponse.EnsureSuccessStatusCode();

        var robots = await activeRobotResponse.Content
            .ReadFromJsonAsync<List<Robot>>();

        Assert.NotNull(robots);
        Assert.NotEmpty(robots);
        Assert.NotNull(robots[0]);

        var request = new RecordWeightRequest
        {
            AnimalId = animal.Id,
            RobotId = robots[0].Id,
            WeightKg = -1m,
            Timestamp = DateTime.UtcNow
        };

        var response = await _client.PostAsJsonAsync(
            RECORD_WEIGTHS_PATH,
            request
        );

        Assert.Equal(
            System.Net.HttpStatusCode.BadRequest,
            response.StatusCode
        );
    }
}