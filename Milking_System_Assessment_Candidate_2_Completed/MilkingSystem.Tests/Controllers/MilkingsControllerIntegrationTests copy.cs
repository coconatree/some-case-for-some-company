using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

using MilkingSystem.Api.Controllers;
using MilkingSystem.Api.Requests;
using MilkingSystem.Api.Responses;

using MilkingSystem.Core.Models;

namespace MilkingSystem.Tests.Controllers;

public class MilkingsControllerTests :
    IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly string CREATE_ANIMAL_PATH = "/api/animals";

    private static readonly string GET_ACTIVE_ROBOT_PATH = "/api/robots/active";

    private static readonly string RECORD_MILKING_PATH = "/api/milkings";

    private static readonly int NON_EXISTENT_ID = int.MaxValue;

    private readonly HttpClient _client;

    public MilkingsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task When_Record_Milking_With_Valid_Data_Then_200()
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

        var firstActiveRobot = robots[0];

        Assert.NotNull(firstActiveRobot);

        var request = new RecordMilkingRequest
        {
            AnimalId = animal.Id,
            RobotId = robots[0].Id,
            MilkYieldLiters = 12.5m,
            Duration = 300
        };

        var response = await _client.PostAsJsonAsync(
            RECORD_MILKING_PATH,
            request
        );

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(
            "Milking successfully recorded!",
            content.Trim('"')
        );
    }

    [Fact]
    public async Task When_Record_Milking_Tries_On_Recently_Milked_Cow_Then_400()
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

        var firstActiveRobot = robots[0];

        Assert.NotNull(firstActiveRobot);

        var request = new RecordMilkingRequest
        {
            AnimalId = animal.Id,
            RobotId = robots[0].Id,
            MilkYieldLiters = 12.5m,
            Duration = 300
        };

        var response = await _client.PostAsJsonAsync(
            RECORD_MILKING_PATH,
            request
        );

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(
            "Milking successfully recorded!",
            content.Trim('"')
        );

        response = await _client.PostAsJsonAsync(
            RECORD_MILKING_PATH,
            request
        );

        Assert.Equal(
            System.Net.HttpStatusCode.BadRequest,
            response.StatusCode
        );

        var error = await response.Content.ReadAsStringAsync();

        Assert.Contains("Cow", error);
        Assert.Contains("recently milked.", error);
    }

    [Fact]
    public async Task When_Two_Milking_Requests_Happen_At_Same_Then_Time_One_Is_Rejected()
    {
        var createAnimalResponse = await _client.PostAsJsonAsync(
            CREATE_ANIMAL_PATH,
            new CreateAnimalRequest
            {
                IdentificationNumber = Guid.NewGuid().ToString()
            });

        createAnimalResponse.EnsureSuccessStatusCode();

        var animal = await createAnimalResponse.Content
            .ReadFromJsonAsync<IdResponse>();

        Assert.NotNull(animal);

        var activeRobotResponse = await _client.GetAsync(GET_ACTIVE_ROBOT_PATH);
        activeRobotResponse.EnsureSuccessStatusCode();

        var robots = await activeRobotResponse.Content
            .ReadFromJsonAsync<List<Robot>>();

        Assert.NotNull(robots);
        Assert.NotEmpty(robots);

        var robot = robots[0];

        var request = new RecordMilkingRequest
        {
            AnimalId = animal.Id,
            RobotId = robot.Id,
            MilkYieldLiters = 10m,
            Duration = 200
        };

        // Send two requests at the same time
        var task1 = _client.PostAsJsonAsync(RECORD_MILKING_PATH, request);
        var task2 = _client.PostAsJsonAsync(RECORD_MILKING_PATH, request);

        await Task.WhenAll(task1, task2);

        var response1 = task1.Result;
        var response2 = task2.Result;

        // One succeeds other fails
        var responses = new[] { response1, response2 };

        var successResponses = responses
            .Where(r => r.StatusCode == System.Net.HttpStatusCode.OK)
            .ToList();

        var badResponses = responses
            .Where(r => r.StatusCode == System.Net.HttpStatusCode.BadRequest)
            .ToList();

        Assert.Single(successResponses);
        Assert.Single(badResponses);

        var errorContent = await badResponses[0].Content.ReadAsStringAsync();

        Assert.Contains("Cow", errorContent);
        Assert.Contains("recently milked", errorContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task When_Animal_Does_Not_Exist_Then_404()
    {
        var activeRobotResponse = await _client.GetAsync(
            GET_ACTIVE_ROBOT_PATH
        );

        activeRobotResponse.EnsureSuccessStatusCode();

        var robots = await activeRobotResponse.Content
            .ReadFromJsonAsync<List<Robot>>();

        Assert.NotNull(robots);
        Assert.NotEmpty(robots);

        var request = new RecordMilkingRequest
        {
            AnimalId = NON_EXISTENT_ID,
            RobotId = robots[0].Id,
            MilkYieldLiters = 12.5m,
            Duration = 300
        };

        var response = await _client.PostAsJsonAsync(
            RECORD_MILKING_PATH,
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

        var request = new RecordMilkingRequest
        {
            AnimalId = animal.Id,
            RobotId = NON_EXISTENT_ID,
            MilkYieldLiters = 12.5m,
            Duration = 300
        };

        var response = await _client.PostAsJsonAsync(
            RECORD_MILKING_PATH,
            request
        );

        Assert.Equal(
            System.Net.HttpStatusCode.NotFound,
            response.StatusCode
        );

        var error = await response.Content.ReadAsStringAsync();

        Assert.Contains(
            "Active robot",
            error,
            StringComparison.OrdinalIgnoreCase
        );

        Assert.Contains("does not exist", error);
    }

    [Fact]
    public async Task When_Milk_Yield_Is_Negative_Then_400()
    {
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

        var activeRobotResponse = await _client.GetAsync(
            GET_ACTIVE_ROBOT_PATH
        );

        activeRobotResponse.EnsureSuccessStatusCode();

        var robots = await activeRobotResponse.Content
            .ReadFromJsonAsync<List<Robot>>();

        Assert.NotNull(robots);
        Assert.NotEmpty(robots);
        Assert.NotNull(robots[0]);

        var request = new RecordMilkingRequest
        {
            AnimalId = animal.Id,
            RobotId = robots[0].Id,
            MilkYieldLiters = -1m,
            Duration = 300
        };

        var response = await _client.PostAsJsonAsync(
            RECORD_MILKING_PATH,
            request
        );

        Assert.Equal(
            System.Net.HttpStatusCode.BadRequest,
            response.StatusCode
        );
    }
}