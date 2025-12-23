using System.Text;
using System.Windows.Media;
using ACARSPlugin.Configuration;
using ACARSPlugin.Model;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ACARSPlugin.ViewModels;

public partial class CurrentMessageViewModel : ObservableObject
{
    private readonly CurrentMessagesConfiguration _config;

    public CurrentMessageViewModel(IAcarsMessageModel message, CurrentMessagesConfiguration config)
    {
        _config = config;
        UpdateMessage(message);
    }

    public IAcarsMessageModel OriginalMessage { get; private set; }

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

        var hasPilotFreeText = false;
        sb.Append(hasPilotFreeText ? "P" : " ");

        return sb.ToString();
    }
}
