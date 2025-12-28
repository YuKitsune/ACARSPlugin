using System.Text;
using System.Windows.Media;
using ACARSPlugin.Configuration;
using ACARSPlugin.Model;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ACARSPlugin.ViewModels;

public partial class HistoryMessageViewModel : ObservableObject
{
    readonly HistoryConfiguration _config;

    public IAcarsMessageModel OriginalMessage { get; }

    public HistoryMessageViewModel(IAcarsMessageModel message, HistoryConfiguration config)
    {
        OriginalMessage = message;
        _config = config;

        Callsign = GetCallsignFromMessage(message);
        Time = FormatTime(GetTimeFromMessage(message));

        var formattedContent = message is UplinkMessage uplinkMessage
            ? uplinkMessage.FormattedContent
            : message.Content;

        Content = GetDisplayText(message, formattedContent);
        FullContent = GetExtendedDisplayText(message, formattedContent);
        Prefix = CalculatePrefix(message);

        var (background, foreground) = MessageColours.GetMessageColors(message);
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

    string FormatTime(DateTimeOffset time)
    {
        return time.ToString("HH:mm");
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

    string GetDisplayText(IAcarsMessageModel message, string fullContent)
    {
        var fullText = GetFormattedContent(message, fullContent);
        if (fullText.Length >= _config.MaxDisplayMessageLength)
            return fullText.Substring(0, _config.MaxDisplayMessageLength);

        return fullText.PadRight(_config.MaxDisplayMessageLength);
    }

    string GetExtendedDisplayText(IAcarsMessageModel message, string fullContent)
    {
        return GetFormattedContent(message, fullContent);
    }

    string GetFormattedContent(IAcarsMessageModel message, string fullContent)
    {
        var contentPrefix = GetMessageContentPrefix(message);
        return contentPrefix + fullContent;
    }

    string GetMessageContentPrefix(IAcarsMessageModel message)
    {
        var sb = new StringBuilder();
        if (message is DownlinkMessage dl)
        {
            // if (dl.HasPilotFreeText)
            // {
            //     sb.Append("P");
            // }
            // else
            // {
            //     sb.Append(" ");
            // }
        }

        sb.Append(message is UplinkMessage { CanAction: true, Actioned: false } ? "X" : " ");
        sb.Append(message is UplinkMessage { IsManuallyAcknowledged: true } ? "M" : " ");

        var isHighPriority = false;
        sb.Append(isHighPriority ? "!" : " ");

        sb.Append(!string.IsNullOrWhiteSpace(sb.ToString()) ? ":" : " ");
        return sb.ToString();
    }

    string CalculatePrefix(IAcarsMessageModel message)
    {
        var sb = new StringBuilder();
        sb.Append(message.Content.Length > _config.MaxDisplayMessageLength ? "*" : " ");
        return sb.ToString();
    }
}
