namespace CPDLCServer.Services;

public interface IStatisticsService
{
    ServerStatistics GetCurrentStatistics();
}

public class ServerStatistics
{
    public int TotalConnections { get; set; }
    public int UniqueNetworks { get; set; }
    public int UniqueStations { get; set; }
    public List<ConnectionInfo> Connections { get; set; } = new();
}

public class ConnectionInfo
{
    public required string Network { get; set; }
    public required string StationId { get; set; }
    public required string Callsign { get; set; }
    public required string VatsimCid { get; set; }
}
