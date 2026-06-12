namespace MilkingSystem.Core.Notifications;

public sealed class Subscription : IDisposable
{
    private readonly Action _unsubscribe;
    private bool _isDisposed;

    public Subscription(Action unsubscribe)
    {
        _unsubscribe = unsubscribe;
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _unsubscribe();
        _isDisposed = true;
    }
}