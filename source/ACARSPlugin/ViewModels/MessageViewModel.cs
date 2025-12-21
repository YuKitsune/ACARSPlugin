using System.Text;
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

        var formattedContent = newMessage is UplinkMessage uplinkMessage
            ? uplinkMessage.FormattedContent
            : newMessage.Content;
        FullContent = formattedContent;
        Content = GetDisplayContent(formattedContent);
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
        var callsign = message switch
        {
            DownlinkMessage dl => dl.Sender,
            UplinkMessage ul => ul.Recipient,
            _ => string.Empty
        };

        // Pad callsign to 8 characters so background extends to full width
        return callsign.PadRight(8);
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
        if (fullContent.Length >= _config.MaxDisplayMessageLength)
            return fullContent.Substring(0, _config.MaxDisplayMessageLength);

        // Pad with spaces to reach max length so background extends to full width
        return fullContent.PadRight(_config.MaxDisplayMessageLength);
    }

    private string CalculatePrefix(IAcarsMessageModel message)
    {
        var sb = new StringBuilder();

        sb.Append(message.Content.Length > _config.MaxDisplayMessageLength ? "*" : " ");

        var isHighPriority = false;
        sb.Append(isHighPriority ? "!" : " ");

        var hasFreeText = false;
        sb.Append(hasFreeText ? "P" : " ");

        return sb.ToString();
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
            // Failed and timeout states have highest priority
            UplinkMessage ul when ul.IsTransmissionFailed => Theme.CPDLCFailedColor,
            UplinkMessage ul when ul.IsPilotLate => Theme.CPDLCPilotLateColor,
            DownlinkMessage dl when dl.IsControllerLate => Theme.CPDLCControllerLateColor,

            // Urgent state
            UplinkMessage ul when ul.IsUrgent => Theme.CPDLCUrgentColor,
            DownlinkMessage dl when dl.IsUrgent => Theme.CPDLCUrgentColor,

            // Closed states
            UplinkMessage ul when ul.IsClosed => Theme.CPDLCClosedColor,
            DownlinkMessage dl when dl.IsClosed => Theme.CPDLCClosedColor,

            // Regular states
            UplinkMessage => Theme.CPDLCUplinkColor,
            DownlinkMessage => Theme.CPDLCDownlinkColor,

            _ => Theme.GenericTextColor
        };
    }

    // Invert the colour of unacknowledged messages to draw the attention of the user (swap background and foreground)
    private bool ShouldInvertColours()
    {
        var isAcknowledged = IsMessageAcknowledged();

        // Special cases that use Normal colours before ack:
        // 1. Closed uplink messages always normal colours
        // 2. Special uplink that is pilot late (Special closed timeout) - Normal video before ack
        if (!isAcknowledged)
        {
            if (OriginalMessage is UplinkMessage ul)
            {
                // Closed uplink or special timeout: use Normal video
                if (ul.IsClosed || (ul.IsSpecial && ul.IsPilotLate))
                    return false;
            }
        }

        // Default: Invert before ack, normal after ack
        return !isAcknowledged;
    }

    private SolidColorBrush GetForegroundColor()
    {
        var messageColor = GetMessageColor();
        var invert = ShouldInvertColours();
        return invert ? Theme.CPDLCBackgroundColor : messageColor;
    }

    private SolidColorBrush GetBackgroundColor()
    {
        var messageColor = GetMessageColor();
        var isAcknowledged = IsMessageAcknowledged();
        var invert = ShouldInvertColours();

        // Exception: Failed and timeout messages use CPDLCClosedColor as background after ack (Note 3)
        if (isAcknowledged && IsFailedOrTimeoutMessage())
            return Theme.CPDLCClosedColor;

        return invert ? messageColor : Theme.CPDLCBackgroundColor;
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
