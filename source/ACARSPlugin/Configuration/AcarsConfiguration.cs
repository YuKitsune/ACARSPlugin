namespace ACARSPlugin.Configuration;

public class AcarsConfiguration
{
    public required string ServerEndpoint { get; init; }
    public required string[] Stations { get; init; }
    public required CurrentMessagesConfiguration CurrentMessages { get; init; }
    public required HistoryConfiguration History { get; init; }
    public required int ControllerLateSeconds { get; init; }
    public required int PilotLateSeconds { get; init; }
    public required string[] SpecialDownlinkMessages { get; init; }
    public required string[] SpecialUplinkMessages { get; init; }
}
