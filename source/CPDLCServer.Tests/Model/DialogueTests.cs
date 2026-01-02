using CPDLCServer.Model;

namespace CPDLCServer.Tests.Model;

public class DialogueTests
{
    [Fact]
    public void Constructor_AddsFirstMessage()
    {
        // Arrange
        var time = DateTimeOffset.UtcNow;
        var downlink = new DownlinkMessage(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST CLIMB FL410",
            time);

        // Act
        var dialogue = new Dialogue("VATSIM", "YBBB", "UAL123", downlink);

        // Assert
        Assert.Single(dialogue.Messages);
        Assert.Equal(downlink, dialogue.Messages[0]);
    }

    [Fact]
    public void Constructor_SetsOpenedTimeToFirstMessageTime()
    {
        // Arrange
        var time = new DateTimeOffset(2025, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var downlink = new DownlinkMessage(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST CLIMB FL410",
            time);

        // Act
        var dialogue = new Dialogue("VATSIM", "YBBB", "UAL123", downlink);

        // Assert
        Assert.Equal(time, dialogue.Opened);
    }

    [Fact]
    public void Constructor_SetsCallsignFromParameter()
    {
        // Arrange
        var time = DateTimeOffset.UtcNow;
        var downlink = new DownlinkMessage(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST CLIMB FL410",
            time);

        // Act
        var dialogue = new Dialogue("VATSIM", "YBBB", "UAL123", downlink);

        // Assert
        Assert.Equal("UAL123", dialogue.AircraftCallsign);
        Assert.Equal("VATSIM", dialogue.FlightSimulationNetwork);
        Assert.Equal("YBBB", dialogue.StationIdentifier);
    }

    [Fact]
    public void AddMessage_UplinkResponseClosesReferencedDownlink()
    {
        // Arrange
        var time = DateTimeOffset.UtcNow;
        var downlink = new DownlinkMessage(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST CLIMB FL410",
            time);

        var dialogue = new Dialogue("VATSIM", "YBBB", "UAL123", downlink);

        var uplink = new UplinkMessage(
            2,
            1, // References downlink message 1
            "UAL123",
            "SYSTEM",
            CpdlcUplinkResponseType.NoResponse,
            AlertType.None,
            "UNABLE",
            time.AddSeconds(10));

        // Act
        dialogue.AddMessage(uplink);

        // Assert
        Assert.True(downlink.IsClosed);
        Assert.True(downlink.IsAcknowledged);
    }

    [Fact]
    public void AddMessage_DownlinkResponseClosesReferencedUplink()
    {
        // Arrange
        var time = DateTimeOffset.UtcNow;
        var uplink = new UplinkMessage(
            1,
            null,
            "UAL123",
            "SYSTEM",
            CpdlcUplinkResponseType.WilcoUnable,
            AlertType.None,
            "CLIMB TO FL410",
            time);

        var dialogue = new Dialogue("VATSIM", "YBBB", "UAL123", uplink);

        var downlink = new DownlinkMessage(
            2,
            1, // References uplink message 1
            "UAL123",
            CpdlcDownlinkResponseType.NoResponse,
            AlertType.None,
            "WILCO",
            time.AddSeconds(10));

        // Act
        dialogue.AddMessage(downlink);

        // Assert
        Assert.True(uplink.IsClosed);
        Assert.True(uplink.IsAcknowledged);
    }

    [Fact]
    public void AddMessage_StandbyDownlinkDoesNotCloseUplink()
    {
        // Arrange
        var time = DateTimeOffset.UtcNow;
        var uplink = new UplinkMessage(
            1,
            null,
            "UAL123",
            "SYSTEM",
            CpdlcUplinkResponseType.WilcoUnable,
            AlertType.None,
            "CLIMB FL410",
            time);

        var dialogue = new Dialogue("VATSIM", "YBBB", "UAL123", uplink);

        var standbyDownlink = new DownlinkMessage(
            2,
            1, // References uplink message 1
            "UAL123",
            CpdlcDownlinkResponseType.NoResponse,
            AlertType.None,
            "STANDBY",
            time.AddSeconds(10));

        // Act
        dialogue.AddMessage(standbyDownlink);

        // Assert
        Assert.False(uplink.IsClosed);
    }

    [Fact]
    public void AddMessage_StandbyUplinkDoesNotCloseDownlink()
    {
        // Arrange
        var time = DateTimeOffset.UtcNow;
        var downlink = new DownlinkMessage(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST CLIMB FL410",
            time);

        var dialogue = new Dialogue("VATSIM", "YBBB", "UAL123", downlink);

        var standbyUplink = new UplinkMessage(
            2,
            1, // References downlink message 1
            "UAL123",
            "SYSTEM",
            CpdlcUplinkResponseType.NoResponse,
            AlertType.None,
            "STANDBY",
            time.AddSeconds(10));

        // Act
        dialogue.AddMessage(standbyUplink);

        // Assert
        Assert.False(downlink.IsClosed);
        Assert.False(downlink.IsAcknowledged);
    }

    [Fact]
    public void AddMessage_RequestDeferredUplinkDoesNotCloseDownlink()
    {
        // Arrange
        var time = DateTimeOffset.UtcNow;
        var downlink = new DownlinkMessage(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST CLIMB FL410",
            time);

        var dialogue = new Dialogue("VATSIM", "YBBB", "UAL123", downlink);

        var deferredUplink = new UplinkMessage(
            2,
            1, // References downlink message 1
            "UAL123",
            "SYSTEM",
            CpdlcUplinkResponseType.NoResponse,
            AlertType.None,
            "REQUEST DEFERRED",
            time.AddSeconds(10));

        // Act
        dialogue.AddMessage(deferredUplink);

        // Assert
        Assert.False(downlink.IsClosed);
        Assert.False(downlink.IsAcknowledged);
    }

    [Fact]
    public void Dialogue_ClosesWhenAllMessagesAreClosed()
    {
        // Arrange
        var time = DateTimeOffset.UtcNow;
        var downlink = new DownlinkMessage(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST CLIMB FL410",
            time);

        var dialogue = new Dialogue("VATSIM", "YBBB", "UAL123", downlink);

        var uplink = new UplinkMessage(
            2,
            1, // References downlink message 1
            "UAL123",
            "BN-TSN_FSS",
            CpdlcUplinkResponseType.NoResponse, // No response required, so self-closing
            AlertType.None,
            "UNABLE",
            time.AddSeconds(10));

        // Act
        dialogue.AddMessage(uplink);

        // Assert
        Assert.True(dialogue.IsClosed);
        Assert.NotNull(dialogue.Closed);
        Assert.Equal(uplink.Sent, dialogue.Closed);
    }

    [Fact]
    public void Dialogue_RemainsOpenWhenSomeMessagesAreOpen()
    {
        // Arrange
        var time = DateTimeOffset.UtcNow;
        var downlink = new DownlinkMessage(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST CLIMB FL410",
            time);

        var dialogue = new Dialogue("VATSIM", "YBBB", "UAL123", downlink);

        var uplink = new UplinkMessage(
            2,
            1, // References downlink message 1
            "UAL123",
            "SYSTEM",
            CpdlcUplinkResponseType.WilcoUnable, // Requires response, not self-closing
            AlertType.None,
            "CLIMB TO FL410",
            time.AddSeconds(10));

        // Act
        dialogue.AddMessage(uplink);

        // Assert - downlink is closed by uplink response, but uplink requires response so stays open
        Assert.True(downlink.IsClosed);
        Assert.False(uplink.IsClosed);
        Assert.False(dialogue.IsClosed);
        Assert.Null(dialogue.Closed);
    }

    [Fact]
    public void Dialogue_MessageRequiringNoResponseIsSelfClosing()
    {
        // Arrange
        var time = DateTimeOffset.UtcNow;
        var downlink = new DownlinkMessage(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.NoResponse, // No response required
            AlertType.None,
            "POSITION REPORT",
            time);

        // Act
        var dialogue = new Dialogue("VATSIM", "YBBB", "UAL123", downlink);

        // Assert
        Assert.True(downlink.IsClosed);
        Assert.True(dialogue.IsClosed);
        Assert.Equal(time, dialogue.Closed);
    }

    [Fact]
    public void Dialogue_MultipleMessagesAndResponses()
    {
        // Arrange - Simulate a realistic CPDLC exchange
        var time = DateTimeOffset.UtcNow;

        // Pilot requests climb
        var downlink1 = new DownlinkMessage(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST CLIMB FL410",
            time);

        var dialogue = new Dialogue("VATSIM", "YBBB", "UAL123", downlink1);
        Assert.False(dialogue.IsClosed); // Dialogue open - downlink awaiting response

        // Controller sends STANDBY
        var uplink1 = new UplinkMessage(
            2,
            1,
            "UAL123",
            "SYSTEM",
            CpdlcUplinkResponseType.NoResponse,
            AlertType.None,
            "STANDBY",
            time.AddSeconds(5));

        dialogue.AddMessage(uplink1);
        Assert.False(downlink1.IsClosed); // STANDBY doesn't close the request
        Assert.False(dialogue.IsClosed);

        // Instruction issued
        var uplink2 = new UplinkMessage(
            3,
            1,
            "UAL123",
            "SYSTEM",
            CpdlcUplinkResponseType.WilcoUnable,
            AlertType.None,
            "CLIMG TO FL410",
            time.AddSeconds(30));

        dialogue.AddMessage(uplink2);
        Assert.True(downlink1.IsClosed); // Now the request is closed
        Assert.True(downlink1.IsAcknowledged);
        Assert.False(dialogue.IsClosed); // But dialogue still open - uplink needs response

        // Pilot acknowledges
        var downlink2 = new DownlinkMessage(
            4,
            3,
            "UAL123",
            CpdlcDownlinkResponseType.NoResponse,
            AlertType.None,
            "WILCO",
            time.AddSeconds(40));

        dialogue.AddMessage(downlink2);
        Assert.True(uplink2.IsClosed);
        Assert.True(uplink2.IsAcknowledged);
        Assert.True(dialogue.IsClosed); // All messages closed, dialogue closes
        Assert.Equal(downlink2.Received, dialogue.Closed);
    }

    [Fact]
    public void Dialogue_LogonRequestAndAcceptanceClosesImmediately()
    {
        // Arrange
        var time = DateTimeOffset.UtcNow;

        // Pilot sends logon request
        var logonRequest = new DownlinkMessage(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST LOGON",
            time);

        var dialogue = new Dialogue("VATSIM", "YBBB", "UAL123", logonRequest);
        Assert.False(dialogue.IsClosed); // Dialogue open - waiting for response

        // System sends LOGON ACCEPTED (NoResponse type, so self-closing)
        var logonAccepted = new UplinkMessage(
            2,
            1, // References logon request
            "UAL123",
            "SYSTEM",
            CpdlcUplinkResponseType.NoResponse,
            AlertType.None,
            "LOGON ACCEPTED",
            time.AddSeconds(1));

        // Act
        dialogue.AddMessage(logonAccepted);

        // Assert
        Assert.True(logonRequest.IsClosed); // Request is closed by the acceptance
        Assert.True(logonRequest.IsAcknowledged); // Request is auto-acknowledged
        Assert.True(logonAccepted.IsClosed); // Acceptance is self-closing (NoResponse)
        Assert.True(dialogue.IsClosed); // Dialogue closes immediately
        Assert.Equal(logonAccepted.Sent, dialogue.Closed);
        Assert.True(dialogue.IsArchived); // Dialogue is auto-archived with LOGON ACCEPTED
        Assert.Equal(logonAccepted.Sent, dialogue.Archived);
    }
}
