using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using ACARSPlugin.Model;
using ACARSPlugin.Server;
using ACARSPlugin.ViewModels;
using ACARSPlugin.Windows;
using CommunityToolkit.Mvvm.DependencyInjection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using vatsys;
using vatsys.Plugin;
using DownlinkMessage = ACARSPlugin.Controls.DownlinkMessage;

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
    
    public IServiceProvider ServiceProvider { get; private set; }
    
    public SignalRConnectionManager? ConnectionManager { get; private set; }

    public Plugin()
    {
        try
        {
            ConfigureServices();
            ConfigureTheme();
            AddToolbarItems();

            Network.Connected += NetworkConnected;
            Network.Disconnected += NetworkDisconnected;
        }
        catch (Exception ex)
        {
            Errors.Add(ex, Name);
        }
    }

    void ConfigureServices()
    {
        ServiceProvider = new ServiceCollection()
            .AddSingleton<MessageRepository>()
            .AddMediatR(c =>
            {
                c.RegisterServicesFromAssemblies(typeof(Plugin).Assembly);
            })
            .BuildServiceProvider();
    }

    private async void NetworkConnected(object sender, EventArgs e)
    {
        try
        {
            if (ConnectionManager is not null)
                ConnectionManager.Dispose();
        
            var mediator =  ServiceProvider.GetService<IMediator>();
            var @delegate = new MediatorMessageHandler(mediator);
            
            ConnectionManager = new SignalRConnectionManager(@delegate);
            await ConnectionManager.InitializeAsync("YBBB", Network.Callsign);
            await ConnectionManager.StartAsync();
        }
        catch (Exception ex)
        {
            Errors.Add(ex, Name);
        }
    }

    private async void NetworkDisconnected(object sender, EventArgs e)
    {
        try
        {
            if (ConnectionManager is null)
                return;

            await ConnectionManager.StopAsync();
            ConnectionManager.Dispose();
            ConnectionManager = null;
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
    
    void AddToolbarItems()
    {
        const string menuItemCategory = "CPDLC";
        
        var setupMenuItem = new CustomToolStripMenuItem(
            CustomToolStripMenuItemWindowType.Main,
            menuItemCategory,
            new ToolStripMenuItem("Setup"));
        setupMenuItem.Item.Click += (_, _) => OpenSetupWindow();

        MMI.AddCustomMenuItem(setupMenuItem);
        
        var historyMenuItem = new CustomToolStripMenuItem(
            CustomToolStripMenuItemWindowType.Main,
            menuItemCategory,
            new ToolStripMenuItem("History"));
        historyMenuItem.Item.Click += (_, _) => OpenHistoryWindow();

        MMI.AddCustomMenuItem(historyMenuItem);
    }
    
    public void OnFDRUpdate(FDP2.FDR updated) { }

    public void OnRadarTrackUpdate(RDP.RadarTrack updated) {}
    
    public CustomLabelItem? GetCustomLabelItem(string itemType, Track track, FDP2.FDR flightDataRecord, RDP.RadarTrack radarTrack)
    {
        if (itemType is not ("ACARSPlugin_CPDLCStatus" or "LABEL_ITEM_CPDLC_STATUS"))
            return null;
        
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
            item.OnMouseClick = _ => 
            {
                // TODO: Better async stuff here
                OpenCpdlcWindow(info.Callsign, CancellationToken.None).GetAwaiter().GetResult();
            };
        }

        if (info.Connected && info.IsCurrentDataAuthority)
        {
            item.Text = "+";
            item.OnMouseClick = _ =>
            {
                // TODO: Better async stuff here
                OpenCpdlcWindow(info.Callsign, CancellationToken.None).GetAwaiter().GetResult();
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

    public CustomColour? SelectASDTrackColour(Track track) => null;

    public CustomColour? SelectGroundTrackColour(Track track) => null;

    AircraftInfo? FindAircraftInfo(string callsign) => new()
    {
        Callsign = callsign,
        Connected = true,
        Equipped = true,
        HasJurisdiction = true,
        IsCurrentDataAuthority = true
    };

    void OpenSetupWindow() => throw new NotImplementedException();
    void OpenHistoryWindow() => throw new NotImplementedException();
    
    async Task OpenCpdlcWindow(string callsign, CancellationToken cancellationToken)
    {
        var repository = ServiceProvider.GetRequiredService<MessageRepository>();
        
        var downlinkMessages = await repository.GetDownlinkMessagesFrom(callsign, cancellationToken);
        var downlinkMessageViewModels = downlinkMessages
            .Where(m => !m.Completed)
            .Select(m => new DownlinkMessageViewModel(m))
            .ToArray();

        var viewModel = new EditorViewModel(callsign, downlinkMessageViewModels);
        
        var window = new EditorWindow(viewModel);
        window.Show();
    }

    public class AircraftInfo
    {
        public string Callsign { get; set; }
        public bool Equipped { get; set; }
        public bool Connected { get; set; }
        public bool HasJurisdiction { get; set; }
        public bool IsCurrentDataAuthority { get; set; }
        public DownlinkMessage? DownlinkMessage { get; set; }
        public bool HasSuspendedMessage { get; set; }
        public bool Unable { get; set; }
    }
}