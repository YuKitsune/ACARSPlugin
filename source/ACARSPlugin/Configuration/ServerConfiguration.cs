using System.Text.Json.Serialization;

namespace ACARSPlugin.Configuration;

public class ServerConfiguration
{
    public required string ServerEndpoint { get; set; }
    
    [JsonIgnore]
    public string StationId { get; set; }
}