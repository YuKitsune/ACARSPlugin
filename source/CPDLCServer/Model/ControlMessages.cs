namespace CPDLCServer.Model;

public static class ControlMessages
{
    public static bool IsLogonRequest(DownlinkMessage downlinkMessage)
    {
        return downlinkMessage.Content == "REQUEST LOGON";
    }

    public static bool IsLogoffNotice(DownlinkMessage downlinkMessage)
    {
        return downlinkMessage.Content == "LOGOFF";
    }
}