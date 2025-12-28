using Serilog.Events;

namespace ACARSPlugin.Configuration;

public class AcarsConfiguration
{
    public required string ServerEndpoint { get; init; }
    public required string[] Stations { get; init; }
    public required CurrentMessagesConfiguration CurrentMessages { get; init; }
    public required HistoryConfiguration History { get; init; }
    public required int ControllerLateSeconds { get; init; } = 120;
    public required int PilotLateSeconds { get; init; } = 120;
    public required string[] SpecialDownlinkMessages { get; init; }
    public required string[] SpecialUplinkMessages { get; init; }
    public required UplinkMessagesConfiguration UplinkMessages { get; init; }
    public int MaxLogFileAgeDays { get; init; } = 5;
    public LogEventLevel LogLevel { get; init; } = LogEventLevel.Information;
}
