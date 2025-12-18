using ACARSPlugin.Server.Contracts;

namespace ACARSPlugin.Model;

public interface IClock
{
    DateTimeOffset UtcNow();
}

public class MessageRepository
{
    readonly List<IAcarsMessageModel> _messages = [];
    readonly SemaphoreSlim _semaphore = new(1, 1);
    readonly IClock _clock;

    public async Task AddDownlinkMessage(ICpdlcDownlink downlinkMessage, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var model = downlinkMessage switch
            {
                CpdlcDownlink cpdlcDownlink => new DownlinkMessage(
                    cpdlcDownlink.MessageId,
                    cpdlcDownlink.Sender,
                    cpdlcDownlink.ResponseType,
                    cpdlcDownlink.Content,
                    _clock.UtcNow()),
                CpdlcDownlinkReply cpdlcDownlinkReply => new DownlinkMessage(
                    cpdlcDownlinkReply.MessageId,
                    cpdlcDownlinkReply.Sender,
                    cpdlcDownlinkReply.ResponseType,
                    cpdlcDownlinkReply.Content,
                    _clock.UtcNow(),
                    cpdlcDownlinkReply.ReplyToMessageId),
                _ => throw new ArgumentOutOfRangeException(nameof(downlinkMessage), downlinkMessage, $"Unexpected downlink message type: {downlinkMessage.GetType().Namespace}")
            };

            _messages.Add(model);
            
            // TODO: Update view models
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task AddUplinkMessage(ICpdlcUplink uplinkMessage, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var model = uplinkMessage switch
            {
                CpdlcUplink cpdlcUplink => new UplinkMessage(
                    cpdlcUplink.MessageId,
                    cpdlcUplink.Recipient,
                    cpdlcUplink.ResponseType,
                    cpdlcUplink.Content,
                    _clock.UtcNow()),
                CpdlcUplinkReply cpdlcUplinkReply => new UplinkMessage(
                    cpdlcUplinkReply.MessageId,
                    cpdlcUplinkReply.Recipient,
                    cpdlcUplinkReply.ResponseType,
                    cpdlcUplinkReply.Content,
                    _clock.UtcNow(),
                    cpdlcUplinkReply.ReplyToMessageId),
                _ => throw new ArgumentOutOfRangeException(nameof(uplinkMessage), uplinkMessage, $"Unexpected uplink message type: {uplinkMessage.GetType().Namespace}")
            };

            _messages.Add(model);
            
            // TODO: Update view models
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IReadOnlyList<DownlinkMessage>> GetDownlinkMessagesFrom(string sender, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return _messages.OfType<DownlinkMessage>()
                .Where(m => m.Sender == sender)
                .OrderBy(m => m.Received)
                .ToArray();
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async Task<IReadOnlyList<IAcarsMessageModel>> GetMessagesFor(string sender, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return _messages
                .Where(IsRelevent)
                .OrderBy(GetDate)
                .ToArray();
        }
        finally
        {
            _semaphore.Release();
        }

        bool IsRelevent(IAcarsMessageModel model)
        {
            return model switch
            {
                DownlinkMessage downlinkMessage => downlinkMessage.Sender == sender,
                UplinkMessage uplinkMessage => uplinkMessage.Recipient == sender,
                _ => throw new ArgumentOutOfRangeException(nameof(model), model, $"Unexpected model type: {model.GetType().Namespace}")
            };
        }

        DateTimeOffset GetDate(IAcarsMessageModel model)
        {
            return model switch
            {
                DownlinkMessage downlinkMessage => downlinkMessage.Received,
                UplinkMessage uplinkMessage => uplinkMessage.Sent,
                _ => throw new ArgumentOutOfRangeException(nameof(model), model, $"Unexpected model type: {model.GetType().Namespace}")
            };
        }
    }
}