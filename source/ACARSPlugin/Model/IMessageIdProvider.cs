namespace ACARSPlugin.Model;

public interface IMessageIdProvider
{
    Task<int> AllocateMessageId(CancellationToken cancellationToken);
}

public class TestMessageIdProvider : IMessageIdProvider
{
    int _messageId = 1000;
    
    public Task<int> AllocateMessageId(CancellationToken cancellationToken)
    {
        return Task.FromResult(Interlocked.Increment(ref _messageId));
    }
}