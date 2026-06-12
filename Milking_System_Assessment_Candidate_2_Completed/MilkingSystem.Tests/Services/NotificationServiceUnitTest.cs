using Microsoft.Extensions.Caching.Memory;
using Xunit;

using MilkingSystem.Core.Notifications;
using MilkingSystem.Core.Services;

namespace MilkingSystem.Tests.Services;

public class NotificationServiceUnitTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly DataService _dataService;

    private readonly IMemoryCache _cache;

    public NotificationServiceUnitTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _dataService = new DataService(_fixture.ConnectionString);
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    [Fact]
    public async Task When_Notification_Published_Then_Subscriber_Executes()
    {
        var notificationService = new InMemoryRobotNotifier(_dataService, _cache);

        var subscriberList = notificationService.GetSubscriberListSnapshot();

        Assert.NotNull(subscriberList);
        Assert.Empty(subscriberList);

        var counter = 0;

        var subcriber01 = notificationService.Subscribe((notfication) => counter++);
        var subcriber02 = notificationService.Subscribe((notfication) => counter++);
        var subcriber03 = notificationService.Subscribe((notfication) => counter++);

        var notification = new MilkingNotification
        {
            AnimalId = 123,
            RobotId = 1,
            Timestamp = DateTime.UtcNow,
            AnimalIdentificationNumber = "RIB-EYE-001"
        };

        notificationService.NotifyMilkingCompleted(notification);

        await Task.Delay(1000);

        Assert.Equal(3, counter);
    }

    [Fact]
    public async Task When_Notification_Published_And_Subscriber_Fails_Then_Retry()
    {
        var notificationService = new InMemoryRobotNotifier(_dataService, _cache);

        var animalId = Random.Shared.Next(1, int.MaxValue);

        var failUntilCount = 0;
        var executionFlag = 0;

        var notification = new MilkingNotification
        {
            AnimalId = animalId,
            RobotId = 1,
            Timestamp = DateTime.UtcNow,
            AnimalIdentificationNumber = "RIB-EYE-001"
        };

        notificationService.Subscribe(n =>
        {
            var attempt = Interlocked.Increment(ref failUntilCount);

            // Hardcoded 2 as the max failure count.
            // Again, bad practice, as the default value 3 can be later changed to 1!
            // I am out of time.
            if (attempt <= 2)
                throw new Exception("Simulated failure");

            Interlocked.Exchange(ref executionFlag, 1);
        });

        notificationService.NotifyMilkingCompleted(notification);

        // Bad practice, as who knows it will only take 3 seconds.
        // Also increases the test execution time.
        // An alternative solution could be to use a pattern like: await().atMost(3, SECONDS).until(() => Assert.Equal(1, executionFlag));
        await Task.Delay(3000);
        Assert.Equal(1, executionFlag);
    }

    [Fact]
    public void When_Subscriber_Dispose_Called_Then_Successfully_Removed()
    {
        var notificationService = new InMemoryRobotNotifier(_dataService, _cache);

        var subscriberList = notificationService.GetSubscriberListSnapshot();

        Assert.NotNull(subscriberList);
        Assert.Empty(subscriberList);

        var subcriber = notificationService.Subscribe((notfication) => Console.WriteLine("Notification recieved!"));

        subscriberList = notificationService.GetSubscriberListSnapshot();

        Assert.NotNull(subscriberList);
        Assert.NotEmpty(subscriberList);

        subcriber.Dispose();
        subscriberList = notificationService.GetSubscriberListSnapshot();

        Assert.NotNull(subscriberList);
        Assert.Empty(subscriberList);
    }

    [Fact]
    public void When_Notify_Called_Then_Cache_Stores_Timestamp()
    {
        var notificationService = new InMemoryRobotNotifier(_dataService, _cache);

        var timestamp = DateTime.UtcNow;

        var animalId = Random.Shared.Next(1, int.MaxValue);

        var notification = new MilkingNotification
        {
            AnimalId = animalId,
            RobotId = 1,
            Timestamp = timestamp,
            AnimalIdentificationNumber = "RIB-EYE-001"
        };

        notificationService.NotifyMilkingCompleted(notification);

        Assert.True(notificationService.WasRecentlyMilked(animalId));

        // Not a huge fan of using reflection, but still.
        var cacheField = typeof(InMemoryRobotNotifier)
            .GetField("_cache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(cacheField);

        var cache = cacheField!.GetValue(notificationService) as Microsoft.Extensions.Caching.Memory.IMemoryCache;

        Assert.NotNull(cache);

        var exists = cache!.TryGetValue<DateTime>(animalId, out var storedTimestamp);

        Assert.True(exists);

        Assert.Equal(timestamp, storedTimestamp);
    }
}