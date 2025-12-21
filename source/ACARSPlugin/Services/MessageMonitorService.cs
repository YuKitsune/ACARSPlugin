using ACARSPlugin.Configuration;
using ACARSPlugin.Messages;
using ACARSPlugin.Model;
using ACARSPlugin.Server.Contracts;
using MediatR;

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
    private readonly Task _monitorTask;
    private readonly CancellationTokenSource _monitorCancellationTokenSource;

    public MessageMonitorService(MessageRepository repository, IClock clock, AcarsConfiguration configuration, IPublisher publisher)
    {
        _repository = repository;
        _clock = clock;
        _configuration = configuration;
        _publisher = publisher;

        _monitorCancellationTokenSource = new CancellationTokenSource();
        _monitorTask = MonitorTask(_monitorCancellationTokenSource.Token);
    }

    async Task MonitorTask(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await CheckForTimeouts(cancellationToken);
                    await ArchiveCompletedDialogues(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Stopping
                }
                catch (Exception ex)
                {
                    // TODO: log
                }
                finally
                {
                    await Task.Delay(_configuration.CurrentMessages.TimeoutCheckIntervalSeconds, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Stopping
        }
        catch (Exception ex)
        {
            // TODO: Log
        }
    }

    async Task CheckForTimeouts(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow();
        var anyChanges = false;
        var dialogues = await _repository.GetCurrentDialogues();
        foreach (var dialogue in dialogues)
        {
            foreach (var message in dialogue.Messages)
            {
                switch (message)
                {
                    case UplinkMessage uplink:
                        if (CheckUplinkTimeout(uplink, now))
                            anyChanges = true;
                        break;

                    case DownlinkMessage downlink:
                        if (CheckDownlinkTimeout(downlink, now))
                            anyChanges = true;
                        break;
                }
            }
        }

        if (anyChanges)
        {
            await _publisher.Publish(new CurrentMessagesChanged(), cancellationToken);
        }
    }

    async Task ArchiveCompletedDialogues(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow();
        var anyChanges = false;
        var dialogues = await _repository.GetCurrentDialogues();

        foreach (var dialogue in dialogues)
        {
            if (!dialogue.IsClosed || !dialogue.Messages.OfType<DownlinkMessage>().All(m => m.IsAcknowledged))
                continue;
            
            var archiveTime = dialogue.Closed.Value.AddSeconds(_configuration.CurrentMessages.HistoryTransferDelaySeconds);
            if (now < archiveTime)
                continue;
            
            dialogue.IsInHistory = true;
            anyChanges = true;
        }

        if (anyChanges)
        {
            await _publisher.Publish(new CurrentMessagesChanged(), cancellationToken);
        }
    }

    bool CheckUplinkTimeout(UplinkMessage uplink, DateTimeOffset now)
    {
        // Skip if already in timeout/failed state, or acknowledged
        if (uplink.IsPilotLate || uplink.IsTransmissionFailed || uplink.IsAcknowledged)
            return false;
        
        // No response required
        if (uplink.ResponseType == CpdlcUplinkResponseType.NoResponse)
            return false;
        
        // Check if timeout has been exceeded
        var timeSinceSent = now - uplink.Sent;
        if (timeSinceSent.TotalSeconds < _configuration.ControllerLateSeconds)
            return false;
        
        uplink.IsPilotLate = true;
        return true;
    }

    bool CheckDownlinkTimeout(DownlinkMessage downlink, DateTimeOffset now)
    {
        // Skip if already in timeout state or doesn't require response
        if (downlink.IsControllerLate)
            return false;
        
        // No response required
        if (downlink.ResponseType == CpdlcDownlinkResponseType.NoResponse)
            return false;
        
        // Check if timeout has been exceeded
        var timeSinceReceived = now - downlink.Received;
        if (timeSinceReceived.TotalSeconds < _configuration.ControllerLateSeconds)
            return false;
        
        downlink.IsControllerLate = true;
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        _monitorCancellationTokenSource.Cancel();
        await _monitorTask;
        _monitorTask.Dispose();
        _monitorCancellationTokenSource.Dispose();
    }
}
