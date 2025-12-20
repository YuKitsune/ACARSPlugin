namespace ACARSPlugin.Configuration;

public class AcarsConfiguration
{
    public required ServerConfiguration Server { get; init; }
    public required CurrentMessagesConfiguration CurrentMessages { get; init; }
}
