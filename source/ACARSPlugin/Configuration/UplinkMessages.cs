namespace ACARSPlugin.Configuration;

public class UplinkMessageTemplates
{
    public required Dictionary<string, UplinkMessageTemplate[]> Messages { get; init; } = [];
}

public class UplinkMessageTemplate
{
    public required string Template { get; init; }
    public required UplinkResponseType ResponseType { get; init; }
}

public enum UplinkResponseType
{
    NoResponse,
    WilcoUnable,
    AffirmativeNegative,
    Roger
}