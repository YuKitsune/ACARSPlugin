namespace ACARSPlugin.Model;

public interface IMessageIdProvider
{
    Task<int> AllocateMessageId(CancellationToken cancellationToken);
}

// TODO: The server overrides the ID for uplink messages
//  Refactor the API so that the ID doesn't actually matter.

public class TestMessageIdProvider : IMessageIdProvider
{
    public Task<int> AllocateMessageId(CancellationToken cancellationToken)
    {
        return Task.FromResult(-1);
    }
}