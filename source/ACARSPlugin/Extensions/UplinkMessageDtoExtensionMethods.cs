using ACARSPlugin.Server.Contracts;

namespace ACARSPlugin.Extensions;

public static class UplinkMessageDtoExtensionMethods
{
    public static string FormattedContent(this UplinkMessageDto dto)
    {
        return dto.Content.Replace("@", "")
            .Replace("\r\n", ". ")
            .Replace("\r", ". ")
            .Replace("\n", ". ");
    }
}
