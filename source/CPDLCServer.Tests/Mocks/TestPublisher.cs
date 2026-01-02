using MediatR;

namespace CPDLCServer.Tests.Mocks;

public class TestPublisher : IPublisher
{
    private readonly List<object> _publishedNotifications = [];

    public IReadOnlyList<object> PublishedNotifications => _publishedNotifications.AsReadOnly();

    public Task Publish(INotification notification, CancellationToken cancellationToken = default)
    {
        _publishedNotifications.Add(notification);
        return Task.CompletedTask;
    }

    public Task Publish(object notification, CancellationToken cancellationToken = new CancellationToken())
    {
        _publishedNotifications.Add(notification);
        return Task.CompletedTask;
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        _publishedNotifications.Add(notification);
        return Task.CompletedTask;
    }

    public void Clear()
    {
        _publishedNotifications.Clear();
    }
}
