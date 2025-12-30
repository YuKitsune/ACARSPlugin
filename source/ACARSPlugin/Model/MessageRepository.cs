using ACARSPlugin.Configuration;
using ACARSPlugin.Server.Contracts;
using Serilog;

namespace ACARSPlugin.Model;

public class MessageRepository(IClock clock, AcarsConfiguration configuration, ILogger logger)
    : IDisposable
{
    readonly List<Dialogue> _dialogues = [];
    readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task AddDownlinkMessage(CpdlcDownlink downlinkMessage, string trackingController, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            logger.Debug("Adding downlink message {MessageId} from {Sender} (ReplyTo: {ReplyToUplinkId})",
                downlinkMessage.Id, downlinkMessage.Sender, downlinkMessage.ReplyToUplinkId);

            // TODO: move DTO to Model conversion into handler
            // Replace newlines with ". " so messages display on a single line
            var content = downlinkMessage.Content
                .Replace("\r\n", ". ")
                .Replace("\n", ". ")
                .Replace("\r", ". ");

            var model = new DownlinkMessage(
                downlinkMessage.Id,
                downlinkMessage.Sender,
                downlinkMessage.ResponseType,
                content,
                clock.UtcNow(),
                configuration.SpecialDownlinkMessages.Contains(downlinkMessage.Content),
                downlinkMessage.ReplyToUplinkId);

            AddMessageToDialogue(model, model.Sender, trackingController);
            logger.Information("Downlink message {MessageId} from {Sender} added to repository",
                downlinkMessage.Id, downlinkMessage.Sender);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task AddUplinkMessage(CpdlcUplink uplinkMessage, string controllerCallsign, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            logger.Debug("Adding uplink message {MessageId} to {Recipient} (ReplyTo: {ReplyToDownlinkId})",
                uplinkMessage.Id, uplinkMessage.Recipient, uplinkMessage.ReplyToDownlinkId);

            // TODO: move DTO to Model conversion into handler
            var model = new UplinkMessage(
                uplinkMessage.Id,
                uplinkMessage.Recipient,
                uplinkMessage.ResponseType,
                uplinkMessage.Content,
                clock.UtcNow(),
                configuration.SpecialUplinkMessages.Contains(uplinkMessage.Content),
                uplinkMessage.ReplyToDownlinkId);

            AddMessageToDialogue(model, model.Recipient, controllerCallsign);
            logger.Information("Uplink message {MessageId} to {Recipient} added to repository",
                uplinkMessage.Id, uplinkMessage.Recipient);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    void AddMessageToDialogue(IAcarsMessageModel message, string aircraftCallsign, string controllerCallsign)
    {
        // Find which dialogue this message belongs to
        var dialogue = _dialogues.FirstOrDefault(d => d.AircraftCallsign == aircraftCallsign && d.RootMessageId == message.ReplyToMessageId);

        if (dialogue == null)
        {
            // Create new dialogue (closure rules are applied within the constructor)
            logger.Debug("Creating new dialogue for {Callsign} with root message {RootMessageId}",
                aircraftCallsign, message.Id);
            dialogue = new Dialogue(message.Id, aircraftCallsign, controllerCallsign, message);
            _dialogues.Add(dialogue);
            logger.Debug("New dialogue created, total dialogues: {DialogueCount}", _dialogues.Count);
        }
        else
        {
            // Add to existing dialogue (closure rules are applied within AddMessage)
            logger.Debug("Adding message {MessageId} to existing dialogue for {Callsign} (Root: {RootMessageId})",
                message.Id, aircraftCallsign, message.ReplyToMessageId);
            dialogue.AddMessage(message);
        }
    }

    IAcarsMessageModel? FindMessageById(string recipient, int id)
    {
        foreach (var dialogue in _dialogues.Where(d => d.AircraftCallsign == recipient))
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
            var dialogues = _dialogues
                .Where(d => !d.IsInHistory)
                .OrderBy(d => d.Opened)
                .ToArray();

            logger.Debug("Retrieved {DialogueCount} current dialogues", dialogues.Length);
            return dialogues;
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
            var dialogues = _dialogues
                .Where(d => d.IsInHistory)
                .OrderBy(d => d.Opened)
                .ToArray();

            logger.Debug("Retrieved {DialogueCount} history dialogues", dialogues.Length);
            return dialogues;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IReadOnlyList<Dialogue>> GetHistoryDialoguesFor(string callsign)
    {
        await _semaphore.WaitAsync();
        try
        {
            var dialogues = _dialogues
                .Where(d => d.IsInHistory && d.AircraftCallsign == callsign)
                .OrderBy(d => d.AircraftCallsign)
                .ThenBy(d => d.Closed ?? d.Opened)
                .ToArray();

            logger.Debug("Retrieved {DialogueCount} history dialogues for {Callsign}", dialogues.Length, callsign);
            return dialogues;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    internal async Task AcknowledgeDownlink(string callsign, int messageId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var message = FindMessageById(callsign, messageId);
            if (message is not DownlinkMessage downlinkMessage)
            {
                logger.Warning("Cannot acknowledge downlink {MessageId}: message not found or not a downlink", messageId);
                return;
            }

            logger.Debug("Acknowledging downlink message {MessageId}", messageId);
            downlinkMessage.IsAcknowledged = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    internal async Task ManuallyAcknowledgeUplink(string callsign, int messageId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var message = FindMessageById(callsign, messageId);
            if (message is not UplinkMessage uplinkMessage)
            {
                logger.Warning("Cannot manually acknowledge uplink {MessageId}: message not found or not an uplink", messageId);
                return;
            }

            logger.Information("Manually acknowledging uplink message {MessageId}", messageId);
            uplinkMessage.IsAcknowledged = true;
            uplinkMessage.IsClosed = true;
            uplinkMessage.IsManuallyAcknowledged = true;

            // Close the dialogue
            var dialogue = _dialogues.FirstOrDefault(d => d.Messages.Contains(uplinkMessage));
            if (dialogue != null)
            {
                logger.Debug("Closing dialogue for {Callsign} due to manual acknowledgement", dialogue.AircraftCallsign);
                dialogue.Close(clock.UtcNow());
            }
            else
            {
                logger.Warning("No dialogue found for uplink message {MessageId}", messageId);
            }
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
            return _dialogues.Where(d => d.AircraftCallsign == sender)
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
        logger.Debug("Disposing message repository");
        _semaphore.Dispose();
    }
}
