using System.Text;
using System.Windows.Media;
using ACARSPlugin.Configuration;
using ACARSPlugin.Model;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ACARSPlugin.ViewModels;

public partial class CurrentMessageViewModel : ObservableObject
{
    readonly CurrentMessagesConfiguration _config;

    public CurrentMessageViewModel(IAcarsMessageModel message, CurrentMessagesConfiguration config)
    {
        _config = config;
        UpdateMessage(message);
    }

    public IAcarsMessageModel OriginalMessage { get; set; }

    public void UpdateMessage(IAcarsMessageModel newMessage)
    {
        OriginalMessage = newMessage;

        Callsign = GetCallsignFromMessage(newMessage);
        Time = FormatTime(GetTimeFromMessage(newMessage));

        var formattedContent = newMessage is UplinkMessage uplinkMessage
            ? uplinkMessage.FormattedContent
            : newMessage.Content;
        FullContent = formattedContent;
        Content = GetDisplayContent(formattedContent);
        Prefix = CalculatePrefix(newMessage);
        IsDownlink = newMessage is DownlinkMessage;

        var (background, foreground) = MessageColours.GetMessageColors(OriginalMessage);
        BackgroundColor = background;
        ForegroundColor = foreground;
    }

    [ObservableProperty]
    string callsign = string.Empty;

    [ObservableProperty]
    string time = string.Empty;

    [ObservableProperty]
    string prefix = string.Empty;

    [ObservableProperty]
    string content = string.Empty;

    [ObservableProperty]
    string fullContent = string.Empty;

    [ObservableProperty]
    bool isExtended;

    [ObservableProperty]
    bool isDownlink;

    [ObservableProperty]
    SolidColorBrush foregroundColor = Theme.GenericTextColor;

    [ObservableProperty]
    SolidColorBrush backgroundColor = Theme.BackgroundColor;

    string GetCallsignFromMessage(IAcarsMessageModel message)
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

    DateTimeOffset GetTimeFromMessage(IAcarsMessageModel message)
    {
        return message switch
        {
            DownlinkMessage dl => dl.Received,
            UplinkMessage ul => ul.Sent,
            _ => DateTimeOffset.MinValue
        };
    }

    string FormatTime(DateTimeOffset time)
    {
        return time.ToString("HH:mm");
    }

    string GetDisplayContent(string fullContent)
    {
        if (fullContent.Length >= _config.MaxDisplayMessageLength)
            return fullContent.Substring(0, _config.MaxDisplayMessageLength);

        // Pad with spaces to reach max length so background extends to full width
        return fullContent.PadRight(_config.MaxDisplayMessageLength);
    }

    string CalculatePrefix(IAcarsMessageModel message)
    {
        var sb = new StringBuilder();

        sb.Append(message.Content.Length > _config.MaxDisplayMessageLength ? "*" : " ");

        var isHighPriority = false;
        sb.Append(isHighPriority ? "!" : " ");

        var hasPilotFreeText = false;
        sb.Append(hasPilotFreeText ? "P" : " ");

        return sb.ToString();
    }
}
