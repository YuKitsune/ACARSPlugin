using System.Text;
using ACARSPlugin.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ACARSPlugin.ViewModels;

public partial class UplinkMessageTemplateViewModel : ObservableObject
{
    [ObservableProperty] private string content;
    [ObservableProperty] private UplinkResponseType responseType;

    // TODO: Make these message types and indicators configurable
    [ObservableProperty] private bool isFreeText;
    [ObservableProperty] private bool isRevision;

    [ObservableProperty] private string displayText;
    [ObservableProperty] private int maxCharacters;

    public UplinkMessageReference MessageReference { get; }

    public UplinkMessageTemplateViewModel(string content, UplinkResponseType responseType, bool isFreeText, bool isRevision, UplinkMessageReference messageReference)
    {
        Content = content;
        ResponseType = responseType;
        IsFreeText = isFreeText;
        IsRevision = isRevision;
        MessageReference = messageReference;
        MaxCharacters = 250; // TODO: Calculate based on view width
        DisplayText = GetDisplayText(Content, MaxCharacters, IsFreeText, IsRevision);
    }
    
    static string GetDisplayText(string fullContent, int maxCharacters, bool isFreeText, bool isRevision)
    {
        var sb = new StringBuilder();

        sb.Append(" ");

        if (isFreeText)
        {
            sb.Append("F");
        }
        else if (isRevision)
        {
            sb.Append("E");
        }
        else
        {
            sb.Append(" ");
        }

        if (isFreeText || isRevision)
        {
            sb.Append(":");
        }
        else
        {
            sb.Append(" ");
        }

        var remainingLength = maxCharacters - sb.Length;
        if (fullContent.Length > remainingLength)
        {
            sb.Append(fullContent.Substring(0, remainingLength));
        }
        else
        {
            sb.Append(fullContent);
        }
        
        var totalLength = sb.Length + fullContent.Length;
        if (totalLength > maxCharacters)
        {
            sb[0] = '*';
        }
        
        return sb.ToString();
    }
}