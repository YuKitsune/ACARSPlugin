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
                    clock.UtcNow()),
                CpdlcDownlinkReply cpdlcDownlinkReply => new DownlinkMessage(
                    cpdlcDownlinkReply.MessageId,
                    cpdlcDownlinkReply.Sender,
                    cpdlcDownlinkReply.ResponseType,
                    cpdlcDownlinkReply.Content,
                    clock.UtcNow(),
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
                    clock.UtcNow()),
                CpdlcUplinkReply cpdlcUplinkReply => new UplinkMessage(
                    cpdlcUplinkReply.MessageId,
                    cpdlcUplinkReply.Recipient,
                    cpdlcUplinkReply.ResponseType,
                    cpdlcUplinkReply.Content,
                    clock.UtcNow(),
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
            // Create new dialogue
            dialogue = new Dialogue(rootMessageId, callsign, message);
            _dialogues.Add(dialogue);
        }
        else
        {
            // Add to existing dialogue
            dialogue.AddMessage(message);
        }

        // Apply message closure rules
        ApplyMessageClosureRules(message, dialogue);
    }

    void ApplyMessageClosureRules(IAcarsMessageModel message, Dialogue dialogue)
    {
        switch (message)
        {
            case UplinkMessage uplink:
                // Rule 1: Uplink requiring no response is closed immediately
                if (uplink.ResponseType == CpdlcUplinkResponseType.NoResponse)
                {
                    uplink.IsClosed = true;
                }
                // Rule 4: When uplink reply is sent, close the downlink it's replying to
                else if (uplink.ReplyToDownlinkId.HasValue)
                {
                    // TODO: Store this info on the message model itself
                    // If the uplink is a special uplink (i.e. STANDBY or REQUEST DEFERRED), don't close the downlink
                    if (configuration.SpecialUplinkMessages.Contains(uplink.Content))
                        return;
                    
                    var downlink = dialogue.Messages.OfType<DownlinkMessage>()
                        .FirstOrDefault(dl => dl.Id == uplink.ReplyToDownlinkId.Value);
                    if (downlink != null)
                    {
                        downlink.IsClosed = true;
                        downlink.IsAcknowledged = true; // Auto-acknowledge when replying
                    }
                }
                break;

            case DownlinkMessage downlink:
                // Rule 3: Downlink requiring no response is closed immediately
                if (downlink.ResponseType != CpdlcDownlinkResponseType.ResponseRequired)
                {
                    downlink.IsClosed = true;
                }
                // Rule 2: When downlink reply is received, close the uplink it's replying to
                else if (downlink.ReplyToUplinkId.HasValue)
                {
                    // TODO: Store this info on the message model itself
                    // If the downlink is a special downlink (i.e. STANDBY), don't close the uplink
                    if (configuration.SpecialDownlinkMessages.Contains(downlink.Content))
                        return;
                    
                    var uplink = dialogue.Messages.OfType<UplinkMessage>()
                        .FirstOrDefault(ul => ul.Id == downlink.ReplyToUplinkId.Value);
                    if (uplink != null)
                    {
                        uplink.IsClosed = true;
                        uplink.IsAcknowledged = true; // Auto-acknowledge when pilot responds
                    }
                }
                break;
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
