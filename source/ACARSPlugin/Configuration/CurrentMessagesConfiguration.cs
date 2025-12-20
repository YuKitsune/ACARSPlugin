namespace ACARSPlugin.Configuration;

public class CurrentMessagesConfiguration
{
    public int MaxCurrentMessages { get; init; } = 50;
    public int HistoryTransferDelaySeconds { get; init; } = 10;
    public int PilotResponseTimeoutSeconds { get; init; } = 120;
    public int MaxDisplayMessageLength { get; init; } = 40;
    public int MaxExtendedMessageLength { get; init; } = 80;
}
