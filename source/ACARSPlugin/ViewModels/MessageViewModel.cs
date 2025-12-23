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

        // Get both colors together
        var (background, foreground) = GetMessageColors();
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

        var hasFreeText = false;
        sb.Append(hasFreeText ? "P" : " ");

        return sb.ToString();
    }

    private ColorPair GetMessageColors()
    {
        var background = Theme.CPDLCBackgroundColor;

        if (IsUrgent(OriginalMessage))
        {
            return new ColorPair(background, Theme.CPDLCUrgentColor).InvertIf(!OriginalMessage.IsAcknowledged);
        }

        if (IsFailed(OriginalMessage))
        {
            // Failed message background is CPDLCClosedColor.
            return new ColorPair(Theme.CPDLCClosedColor, Theme.CPDLCFailedColor).InvertIf(!OriginalMessage.IsAcknowledged);
        }
        
        if (IsClosed(OriginalMessage) && OriginalMessage is UplinkMessage)
        {
            return new ColorPair(background, Theme.CPDLCClosedColor);
        }

        if (IsClosed(OriginalMessage) && OriginalMessage is DownlinkMessage)
        {
            return new ColorPair(background, Theme.CPDLCClosedColor).InvertIf(!OriginalMessage.IsAcknowledged);
        }

        // Special closed timeout: Special uplink that timed out before acknowledgement
        // Shows Normal video (not inverted) with pilot late color
        if (OriginalMessage is UplinkMessage { IsSpecial: true, IsClosed: true, IsPilotLate: true, IsAcknowledged: false })
        {
            return new ColorPair(background, Theme.CPDLCPilotLateColor);
        }

        // Special Closed: For a special Uplink Message that is closed by itself
        // After acknowledgement (even if it timed out), show Normal video with CPDLCClosedColor
        // Before acknowledgement (and hasn't timed out), show Inverse video with CPDLCClosedColor
        if (OriginalMessage is UplinkMessage { IsSpecial: true, IsClosed: true } ul)
        {
            return new ColorPair(background, Theme.CPDLCClosedColor).InvertIf(!ul.IsAcknowledged);
        }

        if (IsPilotLate(OriginalMessage))
        {
            // Time Out (pilot or Controller) message background is CPDLCClosedColor
            return new ColorPair(Theme.CPDLCClosedColor, Theme.CPDLCPilotLateColor).InvertIf(!OriginalMessage.IsAcknowledged);
        }

        if (IsControllerLate(OriginalMessage))
        {
            // Time Out (pilot or Controller) message background is CPDLCClosedColor
            return new ColorPair(Theme.CPDLCClosedColor, Theme.CPDLCControllerLateColor).InvertIf(!OriginalMessage.IsAcknowledged);
        }
        
        if (OriginalMessage is DownlinkMessage)
        {
            return new ColorPair(background, Theme.CPDLCDownlinkColor).InvertIf(!OriginalMessage.IsAcknowledged);
        }

        if (OriginalMessage is UplinkMessage)
        {
            return new ColorPair(background, Theme.CPDLCUplinkColor).Invert();
        }
        
        return new ColorPair(background, Theme.CPDLCClosedColor);

        bool IsUrgent(IAcarsMessageModel message)
        {
            return message switch
            {
                DownlinkMessage downlinkMessage => downlinkMessage.IsUrgent,
                UplinkMessage uplinkMessage => uplinkMessage.IsUrgent,
                _ => false
            };
        }

        bool IsFailed(IAcarsMessageModel message)
        {
            return message switch
            {
                DownlinkMessage downlinkMessage => downlinkMessage.Content.StartsWith("ERROR"), // TODO: Move to model
                UplinkMessage uplinkMessage => uplinkMessage.IsTransmissionFailed,
                _ => false
            };
        }

        bool IsPilotLate(IAcarsMessageModel message)
        {
            return message switch
            {
                UplinkMessage uplinkMessage => uplinkMessage.IsPilotLate,
                _ => false
            };
        }

        bool IsControllerLate(IAcarsMessageModel message)
        {
            return message switch
            {
                DownlinkMessage downlinkMessage => downlinkMessage.IsControllerLate,
                _ => false
            };
        }

        bool IsClosed(IAcarsMessageModel message)
        {
            return message switch
            {
                DownlinkMessage downlinkMessage => downlinkMessage.IsClosed,
                UplinkMessage uplinkMessage => uplinkMessage.IsClosed,
                _ => false
            };
        }
    }

    record ColorPair(SolidColorBrush Background, SolidColorBrush Foreground)
    {
        public ColorPair InvertIf(bool condition)
        {
            return condition
                ? Invert()
                : this;
        }

        public ColorPair Invert() => new(Foreground, Background);
    }
}
