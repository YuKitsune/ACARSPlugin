using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Tests.Mocks;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class SendUplinkCommandHandlerTests
{
    [Fact]
    public async Task Handle_SendsMessageToAcarsClient()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var messageIdProvider = new TestMessageIdProvider();
        var dialogueRepository = new TestDialogueRepository();
        var publisher = new TestPublisher();
        var clock = new TestClock();
        var handler = new SendUplinkCommandHandler(
            clientManager,
            messageIdProvider,
            dialogueRepository,
            publisher,
            clock,
            Logger.None);

        var command = new SendUplinkCommand(
            "BN-TSN_FSS",
            "VATSIM",
            "YBBB",
            "UAL123",
            null,
            CpdlcUplinkResponseType.WilcoUnable,
            "CLIMB TO @FL410@");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.UplinkMessage.MessageId);

        var client = await clientManager.GetAcarsClient("VATSIM", "YBBB", CancellationToken.None);
        var testClient = (TestAcarsClient)client;
        Assert.Single(testClient.SentMessages);

        var sentMessage = Assert.IsType<UplinkMessage>(testClient.SentMessages[0]);
        Assert.Equal(1, sentMessage.MessageId);
        Assert.Equal("UAL123", sentMessage.Recipient);
        Assert.Null(sentMessage.MessageReference);
        Assert.Equal(CpdlcUplinkResponseType.WilcoUnable, sentMessage.ResponseType);
        Assert.Equal("CLIMB TO @FL410@", sentMessage.Content);
    }

    [Fact]
    public async Task Handle_SendsReplyToAcarsClient()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var messageIdProvider = new TestMessageIdProvider();
        var dialogueRepository = new TestDialogueRepository();
        var publisher = new TestPublisher();
        var clock = new TestClock();
        var handler = new SendUplinkCommandHandler(
            clientManager,
            messageIdProvider,
            dialogueRepository,
            publisher,
            clock,
            Logger.None);

        var command = new SendUplinkCommand(
            "BN-TSN_FSS",
            "VATSIM",
            "YBBB",
            "UAL123",
            5,
            CpdlcUplinkResponseType.NoResponse,
            "ROGER");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.UplinkMessage.MessageId);

        var client = await clientManager.GetAcarsClient("VATSIM", "YBBB", CancellationToken.None);
        var testClient = (TestAcarsClient)client;
        Assert.Single(testClient.SentMessages);

        var sentMessage = Assert.IsType<UplinkMessage>(testClient.SentMessages[0]);
        Assert.Equal(1, sentMessage.MessageId);
        Assert.Equal("UAL123", sentMessage.Recipient);
        Assert.Equal(5, sentMessage.MessageReference);
        Assert.Equal(CpdlcUplinkResponseType.NoResponse, sentMessage.ResponseType);
        Assert.Equal("ROGER", sentMessage.Content);
    }

    [Fact]
    public async Task Handle_CreatesNewDialogue_ForUplinkWithNoReference()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var messageIdProvider = new TestMessageIdProvider();
        var dialogueRepository = new TestDialogueRepository();
        var publisher = new TestPublisher();
        var clock = new TestClock();
        var handler = new SendUplinkCommandHandler(
            clientManager,
            messageIdProvider,
            dialogueRepository,
            publisher,
            clock,
            Logger.None);

        var command = new SendUplinkCommand(
            "BN-TSN_FSS",
            "VATSIM",
            "YBBB",
            "UAL123",
            null,
            CpdlcUplinkResponseType.WilcoUnable,
            "CLIMB TO FL410");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        var dialogue = await dialogueRepository.FindDialogueForMessage(
            "VATSIM",
            "YBBB",
            "UAL123",
            result.UplinkMessage.MessageId,
            CancellationToken.None);

        Assert.NotNull(dialogue);
        Assert.Single(dialogue.Messages);
        Assert.Equal(result.UplinkMessage, dialogue.Messages[0]);
    }

    [Fact]
    public async Task Handle_AppendsToExistingDialogue_ForUplinkWithReference()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var messageIdProvider = new TestMessageIdProvider();
        var dialogueRepository = new TestDialogueRepository();
        var clock = new TestClock();

        // Create existing dialogue with a downlink
        var downlink = new DownlinkMessage(
            5,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST CLIMB FL410",
            clock.UtcNow());

        var existingDialogue = new Dialogue("VATSIM", "YBBB", "UAL123", downlink);
        await dialogueRepository.Add(existingDialogue, CancellationToken.None);

        var publisher = new TestPublisher();
        var handler = new SendUplinkCommandHandler(
            clientManager,
            messageIdProvider,
            dialogueRepository,
            publisher,
            clock,
            Logger.None);

        var command = new SendUplinkCommand(
            "BN-TSN_FSS",
            "VATSIM",
            "YBBB",
            "UAL123",
            5,
            CpdlcUplinkResponseType.NoResponse,
            "UNABLE");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        var dialogue = await dialogueRepository.FindDialogueForMessage(
            "VATSIM",
            "YBBB",
            "UAL123",
            5,
            CancellationToken.None);

        Assert.NotNull(dialogue);
        Assert.Equal(2, dialogue.Messages.Count);
        Assert.Contains(result.UplinkMessage, dialogue.Messages);
    }
}
