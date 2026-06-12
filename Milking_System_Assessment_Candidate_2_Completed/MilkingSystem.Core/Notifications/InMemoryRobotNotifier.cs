using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

using MilkingSystem.Core.Services;

namespace MilkingSystem.Core.Notifications;

/// <summary>
/// In-memory implementation of IRobotNotifier.
/// 
/// TODO: This implementation is incomplete. Candidates should:
/// 1. Implement the Subscribe method to allow robots to receive notifications
/// 2. Implement NotifyMilkingCompleted to broadcast to all subscribers
/// 3. Implement WasRecentlyMilked to check if an animal was milked within the protection window
/// 4. Ensure thread-safety for concurrent access
/// </summary>
public class InMemoryRobotNotifier : IRobotNotifier
{
    private readonly DataService _dataService;

    private ConcurrentDictionary<Guid, Action<MilkingNotification>> _subscribersDictionary;

    private readonly IMemoryCache _cache;

    public InMemoryRobotNotifier(DataService dataService, IMemoryCache cache)
    {
        _subscribersDictionary = new ConcurrentDictionary<Guid, Action<MilkingNotification>>();
        _cache = cache;
        _dataService = dataService;
    }

    public void NotifyMilkingCompleted(MilkingNotification notification)
    {
        // TODO: Implement broadcasting to all subscribers
        // Consider: What happens if a subscriber throws an exception?
        // Consider: Should this be synchronous or asynchronous?
        // throw new NotImplementedException("Candidate should implement this method");

        if (notification == null)
        {
            throw new Exception("Milking notification cannot be 'null'.");
        }

        // Take a snapshot of the subscribers
        var subscribers = GetSubscriberListSnapshot();

        // Update the cache
        _cache.Set(notification.AnimalId, notification.Timestamp);

        // Execute notifications call
        _ = Task.Run(() => ExecuteNotificationsAsync(
            subscribers,
            notification
        ));
    }

    // For easier unit tests
    private async Task ExecuteNotificationsAsync(
        List<Action<MilkingNotification>> subscribers,
        MilkingNotification notification
    )
    {
        foreach (var subscriber in subscribers)
        {
            await ExecuteWithRetry(subscriber, notification);
        }
    }

    // Should be moved to where the static util functions live. 
    private static async Task ExecuteWithRetry<T>(Action<T> subscriber, T notification, int maxRetries = 3)
    {
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                subscriber(notification);
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Subscriber failed. Attempt {attempt}/{maxRetries}. Error: {ex.Message}");

                if (attempt == maxRetries - 1)
                {
                    Console.WriteLine($"Subscriber failed after {maxRetries} attempts.");

                    // Do some sort of error logging
                    // Logs can be collected by an observability platform
                    // (e.g. Splunk, Grafana)

                    return;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(100 * (attempt + 1)));
            }
        }
    }

    public IDisposable Subscribe(Action<MilkingNotification> handler)
    {
        // TODO: Implement subscription mechanism
        // Return an IDisposable that removes the subscription when disposed
        // throw new NotImplementedException("Candidate should implement this method");

        // Parameter validation

        if (handler == null)
        {
            throw new Exception("handler cannot be 'null'.");
        }

        var subscriptionId = Guid.NewGuid();
        _subscribersDictionary[subscriptionId] = handler;

        return new Subscription(() =>
        {
            _subscribersDictionary.TryRemove(subscriptionId, out _);
        });
    }

    public bool WasRecentlyMilked(int animalId, int protectionWindowHours = 6)
    {
        // TODO: Implement check for recent milking within the protection window
        // This should be thread-safe and efficient
        // throw new NotImplementedException("Candidate should implement this method");

        var cutoff = DateTime.UtcNow.AddHours(-protectionWindowHours);

        if (_cache.TryGetValue<DateTime>(animalId, out var lastMilking))
        {
            return lastMilking >= cutoff;
        }

        var lastMilkingEvent = _dataService.GetLastMilkingForAnimal(animalId);

        if (lastMilkingEvent == null)
        {
            return false;
        }

        // Store in cache
        _cache.Set(
            animalId,
            lastMilkingEvent.Timestamp,
            TimeSpan.FromHours(protectionWindowHours)
        );

        return lastMilkingEvent.Timestamp >= cutoff;
    }

    // Used for testing the IDisposable
    public List<Action<MilkingNotification>> GetSubscriberListSnapshot()
    {
        return _subscribersDictionary.Values.ToList();
    }
}
