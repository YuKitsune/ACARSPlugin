using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Media;
using ACARSPlugin.Controls;
using vatsys;
using vatsys.Plugin;

namespace ACARSPlugin;

[Export(typeof(IPlugin))]
public class Plugin : ILabelPlugin
{
#if DEBUG
    const string Name = "ACARS Plugin - Debug";
#else
    const string Name = "ACARS Plugin";
#endif

    string IPlugin.Name => Name;

    public Plugin()
    {
        try
        {
            ConfigureTheme();
        }
        catch (Exception ex)
        {
            Errors.Add(ex, Name);
        }
    }

    void ConfigureTheme()
    {
        Theme.BackgroundColor = new SolidColorBrush(Colours.GetColour(Colours.Identities.WindowBackground).ToWindowsColor());
        Theme.GenericTextColor = new SolidColorBrush(Colours.GetColour(Colours.Identities.GenericText).ToWindowsColor());
        Theme.InteractiveTextColor = new SolidColorBrush(Colours.GetColour(Colours.Identities.InteractiveText).ToWindowsColor());
        Theme.NonInteractiveTextColor = new SolidColorBrush(Colours.GetColour(Colours.Identities.NonInteractiveText).ToWindowsColor());
        Theme.SelectedButtonColor = new SolidColorBrush(Colours.GetColour(Colours.Identities.WindowButtonSelected).ToWindowsColor());
        Theme.FontFamily = new FontFamily(MMI.eurofont_xsml.FontFamily.Name);
        Theme.FontSize = MMI.eurofont_xsml.Size;
        Theme.FontWeight = MMI.eurofont_xsml.Bold ? FontWeights.Bold : FontWeights.Regular;
    }
    
    public void OnFDRUpdate(FDP2.FDR updated) { }

    public void OnRadarTrackUpdate(RDP.RadarTrack updated) {}
    
    public CustomLabelItem? GetCustomLabelItem(string itemType, Track track, FDP2.FDR flightDataRecord, RDP.RadarTrack radarTrack)
    {
        if (itemType == "ACARSPlugin_CPDLCStatus")
        {
            var item = new CustomLabelItem();
            
            var info = FindAircraftInfo(flightDataRecord.Callsign);
            if (info is null)
            {
                item.Text = " ";
                return item;
            }

            if (info is { Equipped: true, Connected: false })
            {
                item.Text = ".";
                // TODO: Left click will initiate a manual connection
            }

            if (info is { Connected: true, IsCurrentDataAuthority: false })
            {
                item.Text = "-";
                item.OnMouseClick = _ => OpenCpdlcWindow(info.Callsign);
            }

            if (info.Connected && info.IsCurrentDataAuthority)
            {
                item.Text = "+";
                item.OnMouseClick = _ =>
                {
                    if (info.DownlinkMessage is not null)
                    {
                        OpenCpdlcWindow(info.Callsign, info.DownlinkMessage);
                    }
                    else
                    {
                        OpenCpdlcWindow(info.Callsign);
                    }
                };

                // Color only changes for the responsible controller
                if (!info.HasJurisdiction)
                    return item;
                
                if (info.DownlinkMessage is not null)
                {
                    item.BackColourIdentity = Colours.Identities.CPDLCDownlink;
                }
                else if (info.DownlinkMessage is not null && info.Unable && info.HasJurisdiction)
                {
                    var color = Theme.CPDLCUnableDownlinkColor.Color;
                    item.CustomBackColour = new CustomColour(color.R, color.G, color.B, color.A);
                }

                if (info.HasSuspendedMessage)
                {
                    item.ForeColourIdentity = Colours.Identities.CFLHighlight;
                }
            }
            
            return item;
        }

        return null;
    }

    public CustomColour? SelectASDTrackColour(Track track) => null;

    public CustomColour? SelectGroundTrackColour(Track track) => null;
    
    AircraftInfo? FindAircraftInfo(string callsign) => throw new NotImplementedException();

    void OpenCpdlcWindow(string callsign) => throw new NotImplementedException();
    void OpenCpdlcWindow(string callsign, DownlinkMessage downlinkMessage) => throw new NotImplementedException();
    
    public class AircraftInfo
    {
        public string Callsign { get; }
        public bool Equipped { get; set; }
        public bool Connected { get; set; }
        public bool HasJurisdiction { get; set; }
        public bool IsCurrentDataAuthority { get; set; }
        public DownlinkMessage? DownlinkMessage { get; set; }
        public bool HasSuspendedMessage { get; set; }
        public bool Unable { get; set; }
    }
}