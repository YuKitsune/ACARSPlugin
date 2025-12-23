namespace ACARSPlugin.Configuration;

public class HistoryConfiguration
{
    public int MaxHistory { get; init; } = 100;
    public int MaxDisplayMessageLength { get; init; } = 40;
    public int MaxExtendedMessageLength { get; init; } = 80;
}
