using System.Windows.Media;
using ACARSPlugin.Configuration;
using ACARSPlugin.Model;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ACARSPlugin.ViewModels;

public partial class MessageViewModel : ObservableObject
{
    private readonly CurrentMessagesConfiguration _config;

    public MessageViewModel(IAcarsMessageModel message, CurrentMessagesConfiguration config)
    {
        _config = config;
        UpdateMessage(message);
    }

    public IAcarsMessageModel OriginalMessage { get; private set; }

    public void UpdateMessage(IAcarsMessageModel newMessage)
    {
        OriginalMessage = newMessage;

        // Recalculate all properties that depend on the message
        Callsign = GetCallsignFromMessage(newMessage);
        Time = FormatTime(GetTimeFromMessage(newMessage));
        FullContent = newMessage.Content;
        Content = GetDisplayContent(newMessage.Content);
        Prefix = CalculatePrefix(newMessage);
        IsDownlink = newMessage is DownlinkMessage;
        ForegroundColor = GetForegroundColor();
        BackgroundColor = GetBackgroundColor();
    }

    [ObservableProperty]
    private string callsign = string.Empty;

    [ObservableProperty]
    private string time = string.Empty;

    [ObservableProperty]
    private string prefix = string.Empty;

    [ObservableProperty]
    private string content = string.Empty;

    [ObservableProperty]
    private string fullContent = string.Empty;

    [ObservableProperty]
    private bool isExtended;

    [ObservableProperty]
    private bool isDownlink;

    [ObservableProperty]
    private SolidColorBrush foregroundColor = Theme.GenericTextColor;

    [ObservableProperty]
    private SolidColorBrush backgroundColor = Theme.BackgroundColor;

    private string GetCallsignFromMessage(IAcarsMessageModel message)
    {
        return message switch
        {
            DownlinkMessage dl => dl.Sender,
            UplinkMessage ul => ul.Recipient,
            _ => string.Empty
        };
    }

    private DateTimeOffset GetTimeFromMessage(IAcarsMessageModel message)
    {
        return message switch
        {
            DownlinkMessage dl => dl.Received,
            UplinkMessage ul => ul.Sent,
            _ => DateTimeOffset.MinValue
        };
    }

    private string FormatTime(DateTimeOffset time)
    {
        return time.ToString("HH:mm");
    }

    private string GetDisplayContent(string fullContent)
    {
        if (fullContent.Length <= _config.MaxDisplayMessageLength)
            return fullContent;

        return fullContent.Substring(0, _config.MaxDisplayMessageLength);
    }

    private string CalculatePrefix(IAcarsMessageModel message)
    {
        if (message is not DownlinkMessage)
            return string.Empty;

        // For now, only implement overflow detection (*)
        // ! and P prefixes will be added when specification is provided
        if (message.Content.Length > _config.MaxDisplayMessageLength)
            return "*";

        return string.Empty;
    }

    private bool IsMessageAcknowledged()
    {
        return OriginalMessage switch
        {
            DownlinkMessage dl => dl.IsAcknowledged,
            UplinkMessage ul => ul.IsAcknowledged,
            _ => false
        };
    }

    private SolidColorBrush GetMessageColor()
    {
        return OriginalMessage switch
        {
            UplinkMessage ul when ul.IsUrgent => Theme.CPDLCUrgentColor,
            UplinkMessage ul when ul.IsTransmissionFailed => Theme.CPDLCFailedColor,
            UplinkMessage ul when ul.IsPilotLate => Theme.CPDLCPilotLateColor,
            UplinkMessage => Theme.CPDLCUplinkColor,

            DownlinkMessage dl when dl.IsUrgent => Theme.CPDLCUrgentColor,
            DownlinkMessage dl when dl.IsControllerLate => Theme.CPDLCControllerLateColor,
            DownlinkMessage => Theme.CPDLCDownlinkColor,

            _ => Theme.GenericTextColor
        };
    }

    private SolidColorBrush GetForegroundColor()
    {
        var messageColor = GetMessageColor();
        var isAcknowledged = IsMessageAcknowledged();

        // Inverse video (before acknowledgement): foreground = CPDLCBackgroundColor
        // Normal video (after acknowledgement): foreground = message color
        return isAcknowledged ? Theme.CPDLCBackgroundColor : messageColor;
    }

    private SolidColorBrush GetBackgroundColor()
    {
        var messageColor = GetMessageColor();
        var isAcknowledged = IsMessageAcknowledged();

        // Inverse video (before acknowledgement): background = message color
        // Normal video (after acknowledgement): background = CPDLCBackgroundColor
        // Exception: Failed and timeout messages use CPDLCClosedColor as background after ack (Note 3)
        if (isAcknowledged && IsFailedOrTimeoutMessage())
            return Theme.CPDLCClosedColor;

        return isAcknowledged ? messageColor : Theme.CPDLCBackgroundColor;
    }

    private bool IsFailedOrTimeoutMessage()
    {
        return OriginalMessage switch
        {
            UplinkMessage ul => ul.IsTransmissionFailed || ul.IsPilotLate,
            DownlinkMessage dl => dl.IsControllerLate,
            _ => false
        };
    }
}
