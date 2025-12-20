using ACARSPlugin.Configuration;
using ACARSPlugin.Server.Contracts;
using MediatR;

namespace ACARSPlugin.Model;

public class MessageRepository(IClock clock, AcarsConfiguration configuration)
    : IDisposable
{
    private readonly List<Dialogue> _dialogues = [];
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task AddDownlinkMessage(ICpdlcDownlink downlinkMessage, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // TODO: move DTO to Model conversion into handler
            var model = downlinkMessage switch
            {
                CpdlcDownlink cpdlcDownlink => new DownlinkMessage(
                    cpdlcDownlink.MessageId,
                    cpdlcDownlink.Sender,
                    cpdlcDownlink.ResponseType,
                    cpdlcDownlink.Content,
                    clock.UtcNow(),
                    configuration.SpecialDownlinkMessages.Contains(cpdlcDownlink.Content)),
                CpdlcDownlinkReply cpdlcDownlinkReply => new DownlinkMessage(
                    cpdlcDownlinkReply.MessageId,
                    cpdlcDownlinkReply.Sender,
                    cpdlcDownlinkReply.ResponseType,
                    cpdlcDownlinkReply.Content,
                    clock.UtcNow(),
                    configuration.SpecialDownlinkMessages.Contains(cpdlcDownlinkReply.Content),
                    cpdlcDownlinkReply.ReplyToMessageId),
                _ => throw new ArgumentOutOfRangeException(nameof(downlinkMessage), downlinkMessage, $"Unexpected downlink message type: {downlinkMessage.GetType().Namespace}")
            };

            AddMessageToDialogue(model, model.Sender);
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
            // TODO: move DTO to Model conversion into handler
            var model = uplinkMessage switch
            {
                CpdlcUplink cpdlcUplink => new UplinkMessage(
                    cpdlcUplink.MessageId,
                    cpdlcUplink.Recipient,
                    cpdlcUplink.ResponseType,
                    cpdlcUplink.Content,
                    clock.UtcNow(),
                    configuration.SpecialUplinkMessages.Contains(cpdlcUplink.Content)),
                CpdlcUplinkReply cpdlcUplinkReply => new UplinkMessage(
                    cpdlcUplinkReply.MessageId,
                    cpdlcUplinkReply.Recipient,
                    cpdlcUplinkReply.ResponseType,
                    cpdlcUplinkReply.Content,
                    clock.UtcNow(),
                    configuration.SpecialUplinkMessages.Contains(cpdlcUplinkReply.Content),
                    cpdlcUplinkReply.ReplyToMessageId),
                _ => throw new ArgumentOutOfRangeException(nameof(uplinkMessage), uplinkMessage, $"Unexpected uplink message type: {uplinkMessage.GetType().Namespace}")
            };

            AddMessageToDialogue(model, model.Recipient);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void AddMessageToDialogue(IAcarsMessageModel message, string callsign)
    {
        // Find which dialogue this message belongs to
        var rootMessageId = FindRootMessageId(message);
        var dialogue = _dialogues.FirstOrDefault(d => d.Callsign == callsign && d.RootMessageId == rootMessageId);

        if (dialogue == null)
        {
            // Create new dialogue (closure rules are applied within the constructor)
            dialogue = new Dialogue(rootMessageId, callsign, message);
            _dialogues.Add(dialogue);
        }
        else
        {
            // Add to existing dialogue (closure rules are applied within AddMessage)
            dialogue.AddMessage(message);
        }
    }

    private int FindRootMessageId(IAcarsMessageModel message)
    {
        // If this message is not a reply, it's the root
        var replyToId = message switch
        {
            DownlinkMessage dl => dl.ReplyToUplinkId,
            UplinkMessage ul => ul.ReplyToDownlinkId,
            _ => null
        };

        if (replyToId == null)
            return message.Id;

        // Traverse up the chain to find the root
        var current = replyToId.Value;
        var visited = new HashSet<int> { message.Id };

        while (true)
        {
            // Avoid infinite loops
            if (visited.Contains(current))
                return current;

            visited.Add(current);

            // Find the parent message
            var parentMessage = FindMessageById(current);
            if (parentMessage == null)
                return current; // Can't find parent, assume this is root

            var parentReplyToId = parentMessage switch
            {
                DownlinkMessage dl => dl.ReplyToUplinkId,
                UplinkMessage ul => ul.ReplyToDownlinkId,
                _ => null
            };

            if (parentReplyToId == null)
                return current; // Parent is root

            current = parentReplyToId.Value;
        }
    }

    private IAcarsMessageModel? FindMessageById(int id)
    {
        // Search all dialogues
        foreach (var dialogue in _dialogues)
        {
            var message = dialogue.Messages.FirstOrDefault(m => m.Id == id);
            if (message != null)
                return message;
        }

        return null;
    }

    public async Task<IReadOnlyList<Dialogue>> GetCurrentDialogues()
    {
        await _semaphore.WaitAsync();
        try
        {
            return _dialogues
                .Where(d => !d.IsInHistory)
                .OrderBy(d => d.Opened)
                .ToArray();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IReadOnlyList<Dialogue>> GetHistoryDialogues()
    {
        await _semaphore.WaitAsync();
        try
        {
            return _dialogues
                .Where(d => d.IsInHistory)
                .OrderBy(d => d.Opened)
                .ToArray();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    internal async Task AcknowledgeDownlink(int messageId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var message = FindMessageById(messageId);
            if (message is not DownlinkMessage downlinkMessage)
                return;

            downlinkMessage.IsAcknowledged = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    internal async Task ManuallyAcknowledgeUplink(int messageId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var message = FindMessageById(messageId);
            if (message is not UplinkMessage uplinkMessage)
                return;

            uplinkMessage.IsAcknowledged = true;
            uplinkMessage.IsClosed = true; // Manually acknowledged messages are closed
        }
        finally
        {
            _semaphore.Release();
        }
    }

    // Legacy methods for backward compatibility
    public async Task<IReadOnlyList<DownlinkMessage>> GetDownlinkMessagesFrom(string sender, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return _dialogues.Where(d => d.Callsign == sender)
                .SelectMany(d => d.Messages)
                .OfType<DownlinkMessage>()
                .OrderBy(d => d.Received)
                .ToArray();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}
