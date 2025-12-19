using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using ACARSPlugin.Messages;
using ACARSPlugin.Model;
using ACARSPlugin.Server;
using ACARSPlugin.ViewModels;
using ACARSPlugin.Windows;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using vatsys;
using vatsys.Plugin;
using DownlinkMessage = ACARSPlugin.Controls.DownlinkMessage;

namespace ACARSPlugin;

[Export(typeof(IPlugin))]
public class Plugin : ILabelPlugin, IStripPlugin
{
#if DEBUG
    public const string Name = "ACARS Plugin - Debug";
#else
    public const string Name = "ACARS Plugin";
#endif
    
    string IPlugin.Name => Name;

    public IServiceProvider ServiceProvider { get; private set; }

    public string ServerEndpoint { get; private set; } = string.Empty;
    public string ServerApiKey { get; private set; } = string.Empty;
    public string StationIdentifier { get; private set; } = string.Empty;

    public SignalRConnectionManager? ConnectionManager { get; set; }

    public Plugin()
    {
        try
        {
            ConfigureServices();
            ConfigureTheme();
            AddToolbarItems();
            LoadConfiguration();

            Network.Connected += NetworkConnected;
            Network.Disconnected += NetworkDisconnected;
        }
        catch (Exception ex)
        {
            Errors.Add(ex, Name);
        }
    }

    void LoadConfiguration()
    {
        var (serverEndpoint, apiKey, stationIdentifier) = Configuration.ConfigurationStorage.Load();

        // Initialize with default server endpoint on first load
        ServerEndpoint = serverEndpoint ?? "https://acars.eoinmotherway.dev/hubs/controller";
        ServerApiKey = apiKey ?? string.Empty;
        StationIdentifier = stationIdentifier ?? string.Empty;
    }

    public void UpdateConfiguration(string serverEndpoint, string apiKey, string stationIdentifier)
    {
        ServerEndpoint = serverEndpoint;
        ServerApiKey = apiKey;
        StationIdentifier = stationIdentifier;
    }

    void ConfigureServices()
    {
        ServiceProvider = new ServiceCollection()
            .AddSingleton(this) // TODO: Ick... Whatever we're relying on this for, move it into a separate service please.
            .AddSingleton<MessageRepository>()
            .AddMediatR(c => c.RegisterServicesFromAssemblies(typeof(Plugin).Assembly))
            .BuildServiceProvider();
    }

    private async void NetworkConnected(object sender, EventArgs e)
    {
        // TODO: Connect if auto-connect enabled
        
        // try
        // {
        //     if (ConnectionManager is not null)
        //         ConnectionManager.Dispose();
        //
        //     var mediator =  ServiceProvider.GetRequiredService<IMediator>();
        //     var @delegate = new MediatorMessageHandler(mediator);
        //     
        //     ConnectionManager = new SignalRConnectionManager(ServerEndpoint, ServerApiKey, @delegate);
        //     await ConnectionManager.InitializeAsync(StationIdentifier, Network.Callsign);
        //     await ConnectionManager.StartAsync();
        // }
        // catch (Exception ex)
        // {
        //     Errors.Add(ex, Name);
        // }
    }

    private async void NetworkDisconnected(object sender, EventArgs e)
    {
        try
        {
            var mediator =  ServiceProvider.GetService<IMediator>();
            if (mediator is null)
                return;

            await mediator.Send(new DisconnectRequest());
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
        var customItem = GetCustomStripOrLabelItem(itemType, flightDataRecord);
        if (customItem is null)
            return null;

        return new CustomLabelItem
        {
            Text = customItem.Text,
            CustomBackColour = customItem.BackgroundColour.HasValue
                ? new CustomColour(
                    customItem.BackgroundColour.Value.R,
                    customItem.BackgroundColour.Value.G,
                    customItem.BackgroundColour.Value.B)
                : null,
            CustomForeColour = customItem.ForegroundColour.HasValue
                ? new CustomColour(
                    customItem.ForegroundColour.Value.R,
                    customItem.ForegroundColour.Value.G,
                    customItem.ForegroundColour.Value.B)
                : null,
            OnMouseClick = args =>
            {
                var button = args.Button switch
                {
                    CustomLabelItemMouseButton.Left => ItemClickMouseButton.Left,
                    CustomLabelItemMouseButton.Middle => ItemClickMouseButton.Middle,
                    CustomLabelItemMouseButton.Right => ItemClickMouseButton.Right,
                    _ => throw new ArgumentOutOfRangeException()
                };

                customItem.MouseClickCallback(new ItemClickEventArgs(button));
            }
        };
    }

    public CustomStripItem? GetCustomStripItem(string itemType, Track track, FDP2.FDR flightDataRecord, RDP.RadarTrack radarTrack)
    {
        var customItem = GetCustomStripOrLabelItem(itemType, flightDataRecord);
        if (customItem is null)
            return null;

        return new CustomStripItem
        {
            Text = customItem.Text,
            CustomBackColour = customItem.BackgroundColour.HasValue
                ? new CustomColour(
                    customItem.BackgroundColour.Value.R,
                    customItem.BackgroundColour.Value.G,
                    customItem.BackgroundColour.Value.B)
                : null,
            CustomForeColour = customItem.ForegroundColour.HasValue
                ? new CustomColour(
                    customItem.ForegroundColour.Value.R,
                    customItem.ForegroundColour.Value.G,
                    customItem.ForegroundColour.Value.B)
                : null,
            OnMouseClick = args =>
            {
                var button = args.Button switch
                {
                    CustomLabelItemMouseButton.Left => ItemClickMouseButton.Left,
                    CustomLabelItemMouseButton.Middle => ItemClickMouseButton.Middle,
                    CustomLabelItemMouseButton.Right => ItemClickMouseButton.Right,
                    _ => throw new ArgumentOutOfRangeException()
                };

                customItem.MouseClickCallback(new ItemClickEventArgs(button));
            }
        };
    }

    CustomStripOrLabelItem? GetCustomStripOrLabelItem(string itemType, FDP2.FDR flightDataRecord)
    {
        if (itemType is not ("ACARSPlugin_CPDLCStatus" or "LABEL_ITEM_CPDLC_STATUS" or "CPDLCStatus"))
            return null;

        var text = " ";
        Color? backgroundColour = null;
        Color? foregroundColour = null;
        Action<ItemClickEventArgs> action = _ => { };
            
        var info = FindAircraftInfo(flightDataRecord.Callsign);
        if (info is null)
        {
            text = " ";
        }
        else  if (info is { Equipped: true, Connected: false })
        {
            text = ".";
            // TODO: Left click will initiate a manual connection
        }
        else if (info is { Connected: true, IsCurrentDataAuthority: false })
        {
            text = "-";
            action = args => 
            {
                // TODO: Better async stuff here
                if (args.MouseButton == ItemClickMouseButton.Left)
                {
                    OpenCpdlcWindow(info.Callsign, CancellationToken.None).GetAwaiter().GetResult();                    
                }
            };
        }

        else if (info.Connected && info.IsCurrentDataAuthority)
        {
            text = "+";
            action = args => 
            {
                // TODO: Better async stuff here
                if (args.MouseButton == ItemClickMouseButton.Left)
                {
                    OpenCpdlcWindow(info.Callsign, CancellationToken.None).GetAwaiter().GetResult();                    
                }
            };

            // Color only changes for the responsible controller
            if (info.HasJurisdiction)
            {
                if (info.DownlinkMessage is not null)
                {
                    backgroundColour = Theme.CPDLCDownlinkColor.Color;
                }
                else if (info.DownlinkMessage is not null && info.Unable && info.HasJurisdiction)
                {
                    backgroundColour = Theme.CPDLCUnableDownlinkColor.Color;
                }

                if (info.HasSuspendedMessage)
                {
                    foregroundColour = Theme.CPDLCSuspendedColor.Color;
                }
            }
        }

        return new CustomStripOrLabelItem(text, backgroundColour, foregroundColour,  action);
    }

    enum ItemClickMouseButton { Left, Middle, Right }
    
    record ItemClickEventArgs(ItemClickMouseButton MouseButton);
    
    record CustomStripOrLabelItem(
        string Text,
        Color? BackgroundColour,
        Color? ForegroundColour,
        Action<ItemClickEventArgs> MouseClickCallback);

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

    SetupWindow? _setupWindow = null;

    void OpenSetupWindow()
    {
        // If the setup window is already open, close it
        if (_setupWindow is not null)
        {
            _setupWindow.Close();
            _setupWindow = null;
            return;
        }

        // Get the mediator from the service provider
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        // Create the view model with current configuration and connection state
        var isConnected = ConnectionManager?.IsConnected ?? false;
        var viewModel = new SetupViewModel(
            mediator,
            ServerEndpoint,
            ServerApiKey,
            StationIdentifier,
            isConnected);

        // Create and show the window
        var window = new SetupWindow { DataContext = viewModel };
        window.Closed += (_, _) => _setupWindow = null;

        ElementHost.EnableModelessKeyboardInterop(window);

        _setupWindow = window;
        window.Show();
    }


    HistoryWindow? _historyWindow = null;
    void OpenHistoryWindow()
    {
        // If the history window is already open, close it
        if (_historyWindow is not null)
        {
            _historyWindow.Close();
            _historyWindow = null;
            return;
        }

        // Get the mediator from the service provider
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        // TODO: Create the View Model

        // Create and show the window
        var window = new HistoryWindow();
        window.Closed += (_, _) => _historyWindow = null;

        ElementHost.EnableModelessKeyboardInterop(window);

        _historyWindow = window;
        window.Show();
    }

    // TODO: Close the window if the aircraft disconnects from CPDLC, or if the connection to the ACARS Server fails.
    SemaphoreSlim _editorWindowStateSemaphore = new(1,1);
    WindowState? _editorWindowState = null;
    
    async Task OpenCpdlcWindow(string callsign, CancellationToken cancellationToken)
    {
        await _editorWindowStateSemaphore.WaitAsync(cancellationToken);
        
        try
        {
            // If the editor window is already opened for this aircraft, close it
            if (_editorWindowState is not null && _editorWindowState.Callsign == callsign)
            {
                _editorWindowState.Window.Close();
                _editorWindowState = null;
                return;
            }

            // Close the existing window
            if (_editorWindowState is not null)
            {
                _editorWindowState.Window.Close();
                _editorWindowState = null;
            }

            var repository = ServiceProvider.GetRequiredService<MessageRepository>();

            var downlinkMessages = await repository.GetDownlinkMessagesFrom(callsign, cancellationToken);
            var downlinkMessageViewModels = downlinkMessages
                .Where(m => !m.Completed)
                .Select(m => new DownlinkMessageViewModel(m))
                .ToArray();

            var viewModel = new EditorViewModel(callsign, downlinkMessageViewModels);
            var window = new EditorWindow(viewModel);
            ElementHost.EnableModelessKeyboardInterop(window);
            
            _editorWindowState = new WindowState(callsign, window);

            window.Show();
        }
        finally
        {
            _editorWindowStateSemaphore.Release();
        }
    }

    record WindowState(string Callsign, EditorWindow Window);

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