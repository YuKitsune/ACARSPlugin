using CPDLCServer.Clients;
using CPDLCServer.Infrastructure;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using CPDLCServer.Services;
using MediatR;


namespace CPDLCServer.Handlers;

public class SendUplinkCommandHandler(
    IAircraftRepository aircraftRepository,
    IClientManager clientManager,
    IMessageIdProvider messageIdProvider,
    IDialogueRepository dialogueRepository,
    IPublisher publisher,
    IClock clock,
    ILogger logger)
    : IRequestHandler<SendUplinkCommand, SendUplinkResult>
{
    public async Task<SendUplinkResult> Handle(SendUplinkCommand request, CancellationToken cancellationToken)
    {
        var aircraftConnection = await aircraftRepository.Find(request.Recipient, cancellationToken);
        if (aircraftConnection is null)
            throw new Exception($"{request.Recipient} is not connected");

        var client = await clientManager.GetAcarsClient(aircraftConnection.AcarsClientId, cancellationToken);

        var messageId = await messageIdProvider.GetNextMessageId(
            aircraftConnection.AcarsClientId,
            request.Recipient,
            cancellationToken);

        var uplinkMessage = new UplinkMessage(
            messageId,
            request.ReplyToDownlinkId,
            request.Recipient,
            request.Sender,
            request.ResponseType,
            AlertType.None,
            request.Content,
            clock.UtcNow());

        // Add or update the dialogue
        var dialogue = request.ReplyToDownlinkId.HasValue
            ? await dialogueRepository.FindDialogueForMessage(
                request.Recipient,
                request.ReplyToDownlinkId.Value,
                cancellationToken)
            : null;

        if (dialogue is null)
        {
            dialogue = new Dialogue(request.Recipient, uplinkMessage);
            await dialogueRepository.Add(dialogue, cancellationToken);
        }
        else
        {
            dialogue.AddMessage(uplinkMessage);
        }

        // Publish dialogue change notification
        await publisher.Publish(new DialogueChangedNotification(dialogue), cancellationToken);

        await client.Send(uplinkMessage, cancellationToken);
        logger.Information(
            "Sent CPDLC message from {Sender} to {PilotCallsign}",
            request.Sender,
            uplinkMessage.Recipient);

        return new SendUplinkResult(uplinkMessage);
    }
}
