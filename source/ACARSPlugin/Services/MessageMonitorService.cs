using ACARSPlugin.Configuration;
using ACARSPlugin.Messages;
using ACARSPlugin.Model;
using ACARSPlugin.Server.Contracts;
using MediatR;
using Serilog;

namespace ACARSPlugin.Services;

/// <summary>
/// Background service that monitors messages for timeouts and transfers completed dialogues to history.
/// </summary>
public class MessageMonitorService : IAsyncDisposable
{
    private readonly MessageRepository _repository;
    private readonly IClock _clock;
    private readonly AcarsConfiguration _configuration;
    private readonly IPublisher _publisher;
    private readonly ILogger _logger;
    private readonly Task _monitorTask;
    private readonly CancellationTokenSource _monitorCancellationTokenSource;

    public MessageMonitorService(MessageRepository repository, IClock clock, AcarsConfiguration configuration, IPublisher publisher, ILogger logger)
    {
        _repository = repository;
        _clock = clock;
        _configuration = configuration;
        _publisher = publisher;
        _logger = logger;

        _logger.Information("Starting message monitor service");
        _monitorCancellationTokenSource = new CancellationTokenSource();
        _monitorTask = MonitorTask(_monitorCancellationTokenSource.Token);
    }

    async Task MonitorTask(CancellationToken cancellationToken)
    {
        _logger.Debug("Message monitor task started");
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _logger.Debug("Running message monitor iteration");
                    await CheckForTimeouts(cancellationToken);
                    await ArchiveCompletedDialogues(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.Debug("Message monitor task cancellation requested");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error during message monitoring iteration");
                }
                finally
                {
                    await Task.Delay(_configuration.CurrentMessages.TimeoutCheckIntervalSeconds, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.Information("Message monitor task stopped");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Fatal error in message monitor task");
        }
    }

    async Task CheckForTimeouts(CancellationToken cancellationToken)
    {
        // TODO: Verify if this is supposed to apply to message closures or acknowledgements.

        var now = _clock.UtcNow();
        var anyChanges = false;
        var timedOutMessages = new List<string>();
        var dialogues = await _repository.GetCurrentDialogues();

        _logger.Debug("Checking for timeouts in {DialogueCount} dialogues", dialogues.Count);

        foreach (var dialogue in dialogues)
        {
            foreach (var message in dialogue.Messages)
            {
                switch (message)
                {
                    case UplinkMessage uplink:
                        if (CheckUplinkTimeout(uplink, now))
                        {
                            anyChanges = true;
                            timedOutMessages.Add($"Uplink {uplink.Id} to {dialogue.Callsign}");
                        }
                        break;

                    case DownlinkMessage downlink:
                        if (CheckDownlinkTimeout(downlink, now))
                        {
                            anyChanges = true;
                            timedOutMessages.Add($"Downlink {downlink.Id} from {dialogue.Callsign}");
                        }
                        break;
                }
            }
        }

        if (anyChanges)
        {
            _logger.Information("Detected {TimeoutCount} message timeout(s): {Messages}",
                timedOutMessages.Count, string.Join(", ", timedOutMessages));
            await _publisher.Publish(new CurrentMessagesChanged(), cancellationToken);
        }
    }

    async Task ArchiveCompletedDialogues(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow();
        var anyChanges = false;
        var archivedCallsigns = new List<string>();
        var dialogues = await _repository.GetCurrentDialogues();

        _logger.Debug("Checking for completed dialogues to archive");

        foreach (var dialogue in dialogues)
        {
            if (!dialogue.IsClosed || !dialogue.Messages.OfType<DownlinkMessage>().All(m => m.IsAcknowledged))
                continue;

            var archiveTime = dialogue.Closed.Value.AddSeconds(_configuration.CurrentMessages.HistoryTransferDelaySeconds);
            if (now < archiveTime)
                continue;

            _logger.Debug("Archiving dialogue for {Callsign}", dialogue.Callsign);
            dialogue.IsInHistory = true;
            archivedCallsigns.Add(dialogue.Callsign);
            anyChanges = true;
        }

        if (anyChanges)
        {
            _logger.Information("Archived {DialogueCount} completed dialogue(s): {Callsigns}",
                archivedCallsigns.Count, string.Join(", ", archivedCallsigns));
            await _publisher.Publish(new CurrentMessagesChanged(), cancellationToken);
            await _publisher.Publish(new HistoryMessagesChanged(), cancellationToken);
        }
    }

    bool CheckUplinkTimeout(UplinkMessage uplink, DateTimeOffset now)
    {
        // Skip if already closed, in timeout/failed state, or doesn't require response
        if (uplink.IsClosed ||
            uplink.IsPilotLate ||
            uplink.IsTransmissionFailed ||
            uplink.ResponseType == CpdlcUplinkResponseType.NoResponse)
            return false;

        // Check if timeout has been exceeded
        var timeSinceSent = now - uplink.Sent;
        if (timeSinceSent.TotalSeconds < _configuration.ControllerLateSeconds)
            return false;

        _logger.Debug("Uplink message {UplinkId} marked as pilot late (time since sent: {TimeSinceSent}s)",
            uplink.Id, timeSinceSent.TotalSeconds);
        uplink.IsPilotLate = true;
        return true;
    }

    bool CheckDownlinkTimeout(DownlinkMessage downlink, DateTimeOffset now)
    {
        // Skip if already closed, in timeout state, or doesn't require response
        if (downlink.IsClosed ||
            downlink.IsControllerLate ||
            downlink.ResponseType == CpdlcDownlinkResponseType.NoResponse)
            return false;

        // Check if timeout has been exceeded
        var timeSinceReceived = now - downlink.Received;
        if (timeSinceReceived.TotalSeconds < _configuration.ControllerLateSeconds)
            return false;

        _logger.Debug("Downlink message {DownlinkId} marked as controller late (time since received: {TimeSinceReceived}s)",
            downlink.Id, timeSinceReceived.TotalSeconds);
        downlink.IsControllerLate = true;
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        _logger.Information("Stopping message monitor service");
        _monitorCancellationTokenSource.Cancel();
        await _monitorTask;
        _monitorTask.Dispose();
        _monitorCancellationTokenSource.Dispose();
        _logger.Debug("Message monitor service disposed");
    }
}
