using ACARSPlugin.Configuration;
using ACARSPlugin.Messages;
using ACARSPlugin.Server;
using ACARSPlugin.Server.Contracts;
using MediatR;

namespace ACARSPlugin.Model;

public interface IClock
{
    DateTimeOffset UtcNow();
}

public class SystemClock : IClock
{
    public DateTimeOffset UtcNow() => DateTimeOffset.UtcNow;
}

public class MessageRepository : IDisposable
{
    private readonly Dictionary<string, DialogueGroup> _currentDialogueGroups = new();
    private readonly Dictionary<string, DialogueGroup> _historyDialogueGroups = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly IClock _clock;
    private readonly AcarsConfiguration _configuration;
    private readonly IPublisher _publisher;
    private readonly Timer _historyTransferTimer;

    public SignalRConnectionManager? ConnectionManager { get; set; }

    public MessageRepository(IClock clock, AcarsConfiguration configuration, IPublisher publisher)
    {
        _clock = clock;
        _configuration = configuration;
        _publisher = publisher;

        // Check every 10 seconds for dialogues that should be transferred to history
        _historyTransferTimer = new Timer(
            callback: _ => TransferCompletedDialoguesToHistory(),
            state: null,
            dueTime: TimeSpan.FromSeconds(10),
            period: TimeSpan.FromSeconds(10));
    }

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
            var model = uplinkMessage switch
            {
                CpdlcUplink cpdlcUplink => new UplinkMessage(
                    cpdlcUplink.MessageId,
                    cpdlcUplink.Recipient,
                    cpdlcUplink.ResponseType,
                    cpdlcUplink.Content,
                    _clock.UtcNow())
                {
                    ResponseTimeoutAt = cpdlcUplink.ResponseType != CpdlcUplinkResponseType.NoResponse
                        ? _clock.UtcNow().AddSeconds(_configuration.CurrentMessages.PilotResponseTimeoutSeconds)
                        : null
                },
                CpdlcUplinkReply cpdlcUplinkReply => new UplinkMessage(
                    cpdlcUplinkReply.MessageId,
                    cpdlcUplinkReply.Recipient,
                    cpdlcUplinkReply.ResponseType,
                    cpdlcUplinkReply.Content,
                    _clock.UtcNow(),
                    cpdlcUplinkReply.ReplyToMessageId)
                {
                    ResponseTimeoutAt = cpdlcUplinkReply.ResponseType != CpdlcUplinkResponseType.NoResponse
                        ? _clock.UtcNow().AddSeconds(_configuration.CurrentMessages.PilotResponseTimeoutSeconds)
                        : null
                },
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
        // Get or create dialogue group for this aircraft
        if (!_currentDialogueGroups.TryGetValue(callsign, out var dialogueGroup))
        {
            dialogueGroup = new DialogueGroup(callsign);
            _currentDialogueGroups[callsign] = dialogueGroup;
        }

        // Find which dialogue this message belongs to
        var rootMessageId = FindRootMessageId(message);
        var dialogue = dialogueGroup.Dialogues.FirstOrDefault(d => d.RootMessageId == rootMessageId);

        if (dialogue == null)
        {
            // Create new dialogue
            dialogue = new Dialogue(rootMessageId, callsign, message);
            dialogueGroup.AddDialogue(dialogue);
        }
        else
        {
            // Add to existing dialogue
            dialogue.AddMessage(message);
        }

        // Check if dialogue is complete and close it
        if (dialogue.IsComplete() && dialogue.State == DialogueState.Open)
        {
            dialogue.Close();
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
        // Search all dialogue groups (current and history)
        foreach (var group in _currentDialogueGroups.Values.Concat(_historyDialogueGroups.Values))
        {
            foreach (var dialogue in group.Dialogues)
            {
                var message = dialogue.Messages.FirstOrDefault(m => m.Id == id);
                if (message != null)
                    return message;
            }
        }

        return null;
    }

    public async Task<IReadOnlyList<DialogueGroup>> GetCurrentDialogueGroups()
    {
        await _semaphore.WaitAsync();
        try
        {
            return _currentDialogueGroups.Values
                .Where(g => g.HasCurrentMessages())
                .OrderBy(g => g.FirstMessageTime)
                .ToArray();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IReadOnlyList<DialogueGroup>> GetHistoryDialogueGroups()
    {
        await _semaphore.WaitAsync();
        try
        {
            return _historyDialogueGroups.Values
                .OrderByDescending(g => g.FirstMessageTime)
                .ToArray();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void TransferCompletedDialoguesToHistory()
    {
        _semaphore.Wait();
        try
        {
            var now = _clock.UtcNow();
            var toTransfer = new List<(string Callsign, Dialogue Dialogue)>();

            foreach (var kvp in _currentDialogueGroups)
            {
                var callsign = kvp.Key;
                var dialogueGroup = kvp.Value;
                foreach (var dialogue in dialogueGroup.Dialogues)
                {
                    if (dialogue.ShouldTransferToHistory(now, _configuration.CurrentMessages.HistoryTransferDelaySeconds))
                    {
                        toTransfer.Add((callsign, dialogue));
                    }
                }
            }

            foreach (var (callsign, dialogue) in toTransfer)
            {
                MoveDialogueToHistoryInternal(callsign, dialogue);
            }

            if (toTransfer.Any())
            {
                _publisher.Publish(new CurrentMessagesChanged());
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void MoveDialogueToHistoryInternal(string callsign, Dialogue dialogue)
    {
        // Move dialogue from current to history
        if (_currentDialogueGroups.TryGetValue(callsign, out var currentGroup))
        {
            currentGroup.RemoveDialogue(dialogue);

            // Remove group if empty
            if (currentGroup.Dialogues.Count == 0)
            {
                _currentDialogueGroups.Remove(callsign);
            }
        }

        // Add to history
        if (!_historyDialogueGroups.TryGetValue(callsign, out var historyGroup))
        {
            historyGroup = new DialogueGroup(callsign);
            _historyDialogueGroups[callsign] = historyGroup;
        }

        dialogue.State = DialogueState.InHistory;
        historyGroup.AddDialogue(dialogue);
    }

    // public async Task SendStandby(DownlinkMessage message, CancellationToken cancellationToken)
    // {
    //     if (ConnectionManager == null)
    //         throw new InvalidOperationException("Connection manager not set");
    //
    //     var uplink = new CpdlcUplink(
    //         MessageId: GenerateMessageId(),
    //         Recipient: message.Sender,
    //         ResponseType: CpdlcUplinkResponseType.Roger,
    //         Content: "STANDBY");
    //
    //     await ConnectionManager.SendUplink(uplink, cancellationToken);
    //     await AddUplinkMessage(uplink, CancellationToken.None);
    // }
    //
    // public async Task SendDeferred(DownlinkMessage message, CancellationToken cancellationToken)
    // {
    //     if (ConnectionManager == null)
    //         throw new InvalidOperationException("Connection manager not set");
    //
    //     var uplink = new CpdlcUplink(
    //         MessageId: GenerateMessageId(),
    //         Recipient: message.Sender,
    //         ResponseType: CpdlcUplinkResponseType.Roger,
    //         Content: "REQUEST DEFERRED");
    //
    //     await ConnectionManager.SendUplink(uplink, cancellationToken);
    //     await AddUplinkMessage(uplink, CancellationToken.None);
    // }
    //
    // public async Task SendUnable(DownlinkMessage message, CancellationToken cancellationToken)
    // {
    //     if (ConnectionManager == null)
    //         throw new InvalidOperationException("Connection manager not set");
    //
    //     var uplink = new CpdlcUplink(
    //         MessageId: GenerateMessageId(),
    //         Recipient: message.Sender,
    //         ResponseType: CpdlcUplinkResponseType.NoResponse,
    //         Content: "UNABLE");
    //
    //     await ConnectionManager.SendUplink(uplink, cancellationToken);
    //     await AddUplinkMessage(uplink, CancellationToken.None);
    //     message.Complete(unable: true);
    // }
    //
    // public async Task SendUnableDueTraffic(DownlinkMessage message, CancellationToken cancellationToken)
    // {
    //     if (ConnectionManager == null)
    //         throw new InvalidOperationException("Connection manager not set");
    //
    //     var uplink = new CpdlcUplink(
    //         MessageId: GenerateMessageId(),
    //         Recipient: message.Sender,
    //         ResponseType: CpdlcUplinkResponseType.NoResponse,
    //         Content: "UNABLE DUE TO TRAFFIC");
    //
    //     await ConnectionManager.SendUplink(uplink, cancellationToken);
    //     await AddUplinkMessage(uplink, CancellationToken.None);
    //     message.Complete(unable: true);
    // }
    //
    // public async Task SendUnableDueAirspace(DownlinkMessage message, CancellationToken cancellationToken)
    // {
    //     if (ConnectionManager == null)
    //         throw new InvalidOperationException("Connection manager not set");
    //
    //     var uplink = new CpdlcUplink(
    //         MessageId: GenerateMessageId(),
    //         Recipient: message.Sender,
    //         ResponseType: CpdlcUplinkResponseType.NoResponse,
    //         Content: "UNABLE DUE TO AIRSPACE RESTRICTION");
    //
    //     await ConnectionManager.SendUplink(uplink, cancellationToken);
    //     await AddUplinkMessage(uplink, CancellationToken.None);
    //     message.Complete(unable: true);
    // }

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

            uplinkMessage.State = MessageState.Closed;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    internal async Task MoveToHistory(Dialogue dialogue)
    {
        await _semaphore.WaitAsync();
        try
        {
            MoveDialogueToHistoryInternal(dialogue.Callsign, dialogue);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    // public async Task ReissueMessage(UplinkMessage message, CancellationToken cancellationToken)
    // {
    //     if (ConnectionManager == null)
    //         throw new InvalidOperationException("Connection manager not set");
    //
    //     // Create a new message with the same content
    //     var uplink = new CpdlcUplink(
    //         MessageId: GenerateMessageId(),
    //         Recipient: message.Recipient,
    //         ResponseType: message.ResponseType,
    //         Content: message.Content);
    //
    //     await ConnectionManager.SendUplink(uplink, cancellationToken);
    //     await AddUplinkMessage(uplink, CancellationToken.None);
    // }

    // Legacy methods for backward compatibility
    public async Task<IReadOnlyList<DownlinkMessage>> GetDownlinkMessagesFrom(string sender, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var messages = new List<DownlinkMessage>();

            foreach (var group in _currentDialogueGroups.Values)
            {
                if (group.Callsign == sender)
                {
                    messages.AddRange(group.GetAllMessagesSortedByTime().OfType<DownlinkMessage>());
                }
            }

            return messages.OrderBy(m => m.Received).ToArray();
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
            if (_currentDialogueGroups.TryGetValue(sender, out var group))
            {
                return group.GetAllMessagesSortedByTime().ToArray();
            }

            return Array.Empty<IAcarsMessageModel>();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _historyTransferTimer?.Dispose();
        _semaphore?.Dispose();
    }
}
