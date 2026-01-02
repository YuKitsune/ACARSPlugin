namespace CPDLCServer.Model;

public class ControllerInfo(
    Guid userId,
    string connectionId,
    string callsign,
    string vatsimCid)
{
    public Guid UserId { get; } = userId;
    public string ConnectionId { get; } = connectionId;
    public string Callsign { get; } = callsign;
    public string VatsimCid { get; } = vatsimCid;
}
