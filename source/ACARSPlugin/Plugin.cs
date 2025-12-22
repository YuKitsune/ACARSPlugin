using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using ACARSPlugin.Configuration;
using ACARSPlugin.Messages;
using ACARSPlugin.Model;
using ACARSPlugin.Server;
using ACARSPlugin.Server.Contracts;
using ACARSPlugin.Services;
using ACARSPlugin.ViewModels;
using ACARSPlugin.Windows;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using vatsys;
using vatsys.Plugin;

namespace ACARSPlugin;

// TODO: Strip items

[Export(typeof(IPlugin))]
public class Plugin : ILabelPlugin, IRecipient<CurrentMessagesChanged>, IRecipient<ConnectedAircraftChanged>
{
#if DEBUG
    public const string Name = "ACARS Plugin - Debug";
#else
    public const string Name = "ACARS Plugin";
#endif
    
    // Cache for CustomStripOrLabelItem to avoid expensive lookups on every label update
    private readonly ConcurrentDictionary<string, CustomStripOrLabelItem> _labelItemCache = new();
    
    string IPlugin.Name => Name;

    public IServiceProvider ServiceProvider { get; private set; }

    public SignalRConnectionManager? ConnectionManager { get; set; }

    public Plugin()
    {
        try
        {
            var configuration = LoadConfiguration();
            ConfigureServices(configuration);
            ConfigureTheme();
            AddToolbarItems();

            Network.Connected += NetworkConnected;
            Network.Disconnected += NetworkDisconnected;
            
            FDP2.FDRsChanged += FDP2OnFDRsChanged;
            
            WeakReferenceMessenger.Default.Register<CurrentMessagesChanged>(this);
            WeakReferenceMessenger.Default.Register<ConnectedAircraftChanged>(this);
        }
        catch (Exception ex)
        {
            Errors.Add(ex, Name);
        }
    }

    AcarsConfiguration LoadConfiguration()
    {
        var configuration = ConfigurationLoader.Load();
        return configuration;
    }

    ServerConfiguration CreateServerConfiguration(string serverEndpoint)
    {
        return new ServerConfiguration { ServerEndpoint = serverEndpoint };
    }

    void ConfigureServices(AcarsConfiguration acarsConfiguration)
    {
        var serverConfiguration = CreateServerConfiguration(acarsConfiguration.ServerEndpoint);
        
        ServiceProvider = new ServiceCollection()
            .AddSingleton(this) // TODO: Ick... Whatever we're relying on this for, move it into a separate service please.
            .AddSingleton(acarsConfiguration)
            .AddSingleton(serverConfiguration)
            .AddSingleton<IClock>(new SystemClock())
            .AddSingleton<MessageRepository>()
            .AddSingleton<IGuiInvoker, GuiInvoker>()
            .AddSingleton<MessageMonitorService>()
            .AddSingleton<IErrorReporter, ErrorReporter>()
            .AddSingleton<AircraftConnectionTracker>()
            .AddMediatR(c => c.RegisterServicesFromAssemblies(typeof(Plugin).Assembly))
            .BuildServiceProvider();
        
        // Activate the monitor
        // TODO: Do this a better way...
        ServiceProvider.GetRequiredService<MessageMonitorService>();
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
        //
        //     // Set ConnectionManager on repository
        //     var repository = ServiceProvider.GetRequiredService<MessageRepository>();
        //     repository.ConnectionManager = ConnectionManager;
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
        Theme.BackgroundColor = GetColour(Colours.Identities.WindowBackground);
        Theme.GenericTextColor = GetColour(Colours.Identities.GenericText);
        Theme.InteractiveTextColor = GetColour(Colours.Identities.InteractiveText);
        Theme.NonInteractiveTextColor = GetColour(Colours.Identities.NonInteractiveText);
        Theme.SelectedButtonColor = GetColour(Colours.Identities.WindowButtonSelected);

        Theme.CPDLCBackgroundColor = GetColour(Colours.Identities.CPDLCMessageBackground);
        Theme.CPDLCUplinkColor = GetColour(Colours.Identities.CPDLCUplink);
        Theme.CPDLCDownlinkColor = GetColour(Colours.Identities.CPDLCDownlink);
        Theme.CPDLCSendBackgroundColor = GetColour(Colours.Identities.CPDLCSendButton);
        Theme.CPDLCHotButtonBackgroundColor = GetColour(Colours.Identities.CPDLCSendButton);
        
        Theme.FontFamily = new FontFamily(MMI.eurofont_xsml.FontFamily.Name);
        Theme.FontSize = MMI.eurofont_xsml.Size;
        Theme.FontWeight = MMI.eurofont_xsml.Bold ? FontWeights.Bold : FontWeights.Regular;
        
        CacheLabelColours();

        SolidColorBrush GetColour(Colours.Identities identity)
        {
            return new SolidColorBrush(Colours.GetColour(identity).ToWindowsColor());
        }
    }
    
    void CacheLabelColours()
    {
        // Need to cache these for thread-safe access
        
        var downlinkColor = Theme.CPDLCDownlinkColor.Color;
        _cachedDownlinkColor = new CustomColour(downlinkColor.R, downlinkColor.G, downlinkColor.B);

        var unableColor = Theme.CPDLCUnableDownlinkColor.Color;
        _cachedUnableDownlinkColor = new CustomColour(unableColor.R, unableColor.G, unableColor.B);

        var suspendedColor = Theme.CPDLCSuspendedColor.Color;
        _cachedSuspendedColor = new CustomColour(suspendedColor.R, suspendedColor.G, suspendedColor.B);
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
        
        var currentMessagesMenuItem = new CustomToolStripMenuItem(
            CustomToolStripMenuItemWindowType.Main,
            menuItemCategory,
            new ToolStripMenuItem("Current Messages"));
        currentMessagesMenuItem.Item.Click += (_, _) => OpenCurrentMessagesWindow();

        MMI.AddCustomMenuItem(currentMessagesMenuItem);
        
        var historyMenuItem = new CustomToolStripMenuItem(
            CustomToolStripMenuItemWindowType.Main,
            menuItemCategory,
            new ToolStripMenuItem("History"));
        historyMenuItem.Item.Click += (_, _) => OpenHistoryWindow();

        MMI.AddCustomMenuItem(historyMenuItem);
    }
    
    public void OnFDRUpdate(FDP2.FDR updated) { }

    public void OnRadarTrackUpdate(RDP.RadarTrack updated) {}
    
    public CustomLabelItem GetCustomLabelItem(string itemType, Track track, FDP2.FDR flightDataRecord, RDP.RadarTrack radarTrack)
    {
        var labelItem = new CustomLabelItem
        {
            Type = itemType,
            Text = " "
        };
        
        try
        {
            
            var customItem = GetCustomStripOrLabelItem(itemType, flightDataRecord);
            if (customItem is null)
            {
                return labelItem;
            }

            labelItem.Text = customItem.Text;
            
            // BUG: These colours don't change
            if (customItem.ForegroundColour is not null)
            {
                labelItem.ForeColourIdentity = Colours.Identities.Custom;
                labelItem.CustomForeColour = customItem.ForegroundColour;
            }

            if (customItem.BackgroundColour is not null)
            {
                labelItem.BackColourIdentity = Colours.Identities.Custom;
                labelItem.CustomBackColour = customItem.BackgroundColour;
            }

            labelItem.OnMouseClick = args =>
            {
                var action = args.Button switch
                {
                    CustomLabelItemMouseButton.Left => customItem.LeftClickCallback,
                    CustomLabelItemMouseButton.Middle => customItem.MiddleClickCallback,
                    CustomLabelItemMouseButton.Right => customItem.RightClickCallback,
                    _ => throw new ArgumentOutOfRangeException()
                };

                action();
            };

            return labelItem;
        }
        catch (Exception ex)
        {
            var wrappedException = new Exception($"Failed to create custom label item: {ex.Message}", ex);
            Errors.Add(wrappedException, Name);
            return labelItem;
        }
    }

    CustomStripOrLabelItem? GetCustomStripOrLabelItem(string itemType, FDP2.FDR flightDataRecord)
    {
        try
        {
            if (itemType is not ("ACARSPlugin_CPDLCStatus" or "LABEL_ITEM_CPDLC_STATUS" or "CPDLCStatus"))
                return null;

            return _labelItemCache.TryGetValue(flightDataRecord.Callsign, out var cachedItem)
                ? cachedItem
                : null;
        }
        catch (Exception ex)
        {
            Errors.Add(ex, Name);
            return null;
        }
    }
    
    record CustomStripOrLabelItem(
        string Text,
        CustomColour? BackgroundColour,
        CustomColour? ForegroundColour,
        Action LeftClickCallback,
        Action MiddleClickCallback,
        Action RightClickCallback);

    public CustomColour? SelectASDTrackColour(Track track) => null;

    public CustomColour? SelectGroundTrackColour(Track track) => null;

    async Task RebuildLabelItemCache()
    {
        try
        {
            var allExistingKeys = _labelItemCache.Keys;
            var allUpdatedKeys = new List<string>();
            
            _labelItemCache.Clear();

            var repository = ServiceProvider.GetRequiredService<MessageRepository>();
            var aircraftTracker = ServiceProvider.GetRequiredService<AircraftConnectionTracker>();

            var connectedAircraft = await aircraftTracker.GetConnectedAircraft();

            foreach (var flightDataRecord in FDP2.GetFDRs)
            {
                if (flightDataRecord is null)
                    continue;

                var connection = connectedAircraft.FirstOrDefault(c => c.Callsign == flightDataRecord.Callsign);
                    
                var messages = repository.GetDownlinkMessagesFrom(flightDataRecord.Callsign, CancellationToken.None)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
                
                var hasActiveDownlinkMessages = messages.Any(m => !m.IsClosed || !m.IsAcknowledged);
                
                var isEquipped = new[]
                {
                    "J1",
                    "J2",
                    "J3",
                    "J4",
                    "J5",
                    "J6",
                    "J7",
                }.Any(s => flightDataRecord.AircraftEquip.Contains(s));
                
                // TODO: if dialog is open and messages contains UNABLE
                var unableReceived = messages.Any(m => !m.IsAcknowledged && m.Content.Contains("UNABLE"));

                // TODO: Suspended messages
                var hasSuspendedMessage = false;
                
                var guiInvoker = ServiceProvider.GetRequiredService<IGuiInvoker>();

                var text = " ";
                CustomColour? backgroundColour = null;
                CustomColour? foregroundColour = null;
                Action leftClickAction = () => { };
                Action middleClickAction = () => { };
                Action rightClickAction = () => { };

                if (isEquipped && connection is null)
                {
                    text = ".";
                    // TODO: Left click will initiate a manual connection
                }
                else if (connection is not null && connection.DataAuthorityState == DataAuthorityState.NextDataAuthority)
                {
                    text = "-";
                    leftClickAction = () =>
                    {
                        try
                        {
                            // TODO: Better async stuff here
                            guiInvoker.InvokeOnGUI(() =>
                            {
                                OpenCpdlcWindow(flightDataRecord.Callsign, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
                            });
                        }
                        catch (Exception ex)
                        {
                            Errors.Add(ex, Name);
                        }
                    };
                }
                else if (connection is not null && connection.DataAuthorityState == DataAuthorityState.CurrentDataAuthority)
                {
                    text = "+";
                    leftClickAction = () =>
                    {
                        try
                        {
                            // TODO: Better async stuff here
                            guiInvoker.InvokeOnGUI(() =>
                            {
                                OpenCpdlcWindow(flightDataRecord.Callsign, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
                            });
                        }
                        catch (Exception ex)
                        {
                            Errors.Add(ex, Name);
                        }
                    };

                    // Color only changes for the responsible controller
                    if (flightDataRecord.IsTrackedByMe)
                    {
                        if (hasActiveDownlinkMessages && unableReceived)
                        {
                            backgroundColour = _cachedUnableDownlinkColor;
                        }
                        else if (hasActiveDownlinkMessages)
                        {
                            backgroundColour = _cachedDownlinkColor;
                        }

                        // TODO: Verify this behaviour is correct
                        if (hasSuspendedMessage)
                        {
                            foregroundColour = _cachedSuspendedColor;
                        }
                    }
                }

                _labelItemCache[flightDataRecord.Callsign] = new CustomStripOrLabelItem(
                    text,
                    backgroundColour,
                    foregroundColour,
                    leftClickAction,
                    middleClickAction,
                    rightClickAction);
                
                allUpdatedKeys.Add(flightDataRecord.Callsign);
            }
            
            var missingKeys = allExistingKeys.Except(allUpdatedKeys);
            foreach (var missingKey in missingKeys)
            {
                _labelItemCache.TryRemove(missingKey, out _);
            }
        }
        catch (Exception ex)
        {
            Errors.Add(new Exception($"Failed to rebuild label item cache: {ex.Message}", ex), Name);
        }
    }

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

        var acarsConfiguration = ServiceProvider.GetRequiredService<AcarsConfiguration>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        // Create the view model with current configuration and connection state
        var isConnected = ConnectionManager?.IsConnected ?? false;
        var errorReporter = ServiceProvider.GetRequiredService<IErrorReporter>();
        var viewModel = new SetupViewModel(
            mediator,
            errorReporter,
            acarsConfiguration.ServerEndpoint,
            acarsConfiguration.Stations,
            isConnected ? ConnectionManager!.StationIdentifier : acarsConfiguration.Stations.First(),
            isConnected);

        // Create and show the window
        var window = new SetupWindow(viewModel);
        window.Closed += (_, _) => _setupWindow = null;

        ElementHost.EnableModelessKeyboardInterop(window);

        _setupWindow = window;
        window.Show();
    }

    HistoryWindow? _historyWindow = null;
    CurrentMessagesWindow? _currentMessagesWindow = null;

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

    void OpenCurrentMessagesWindow()
    {
        if (_currentMessagesWindow is not null)
            return; // Already open

        var configuration = ServiceProvider.GetRequiredService<AcarsConfiguration>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var guiInvoker = ServiceProvider.GetRequiredService<IGuiInvoker>();
        var errorReporter = ServiceProvider.GetRequiredService<IErrorReporter>();
        var viewModel = new CurrentMessagesViewModel(configuration, mediator, guiInvoker, errorReporter);

        var window = new CurrentMessagesWindow(viewModel);
        window.Closed += (_, _) => _currentMessagesWindow = null;

        ElementHost.EnableModelessKeyboardInterop(window);

        _currentMessagesWindow = window;
        window.Show();
    }

    void CloseCurrentMessagesWindow()
    {
        if (_currentMessagesWindow is null)
            return;

        _currentMessagesWindow.Close();
        _currentMessagesWindow = null;
    }

    private async void FDP2OnFDRsChanged(object sender, FDP2.FDRsChangedEventArgs e)
    {
        try
        {
            await RebuildLabelItemCache();
        }
        catch (Exception ex)
        {
            Errors.Add(ex, Name);
        }
    }

    public async void Receive(ConnectedAircraftChanged _)
    {
        try
        {
            await RebuildLabelItemCache();
        }
        catch (Exception ex)
        {
            Errors.Add(ex, Name);
        }
    }
    
    public async void Receive(CurrentMessagesChanged _)
    {
        try
        {
            await RebuildLabelItemCache();

            var repository = ServiceProvider.GetRequiredService<MessageRepository>();
            var currentDialogues = await repository.GetCurrentDialogues();
            var guiInvoker = ServiceProvider.GetRequiredService<IGuiInvoker>();

            if (currentDialogues.Any())
            {
                if (_currentMessagesWindow is null)
                    guiInvoker.InvokeOnGUI(OpenCurrentMessagesWindow);
            }
            else
            {
                guiInvoker.InvokeOnGUI(CloseCurrentMessagesWindow);
            }
        }
        catch (Exception ex)
        {
            Errors.Add(ex, Name);
        }
    }

    // Cached theme colors to avoid deadlock when accessing from non-GUI threads
    CustomColour _cachedDownlinkColor = new(0, 105, 0);
    CustomColour _cachedUnableDownlinkColor = new(230, 127, 127);
    CustomColour _cachedSuspendedColor = new(255, 255, 255);

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

            var mediator = ServiceProvider.GetRequiredService<IMediator>();

            var response = await mediator.Send(new GetCurrentDialoguesRequest(), cancellationToken);

            var downlinkMessageViewModels = new List<DownlinkMessageViewModel>();
            
            foreach (var dialogue in response.Dialogues)
            {
                var firstMessage = dialogue.Messages.First();
                if (firstMessage is not DownlinkMessage downlinkMessage)
                    continue;

                var downlinkMessageViewModel = new DownlinkMessageViewModel(
                    downlinkMessage,
                    standbySent: dialogue.HasStandbyResponse(downlinkMessage.Id),
                    deferred: dialogue.HasDeferredResponse(downlinkMessage.Id));
                
                downlinkMessageViewModels.Add(downlinkMessageViewModel);
            }

            var errorReporter = ServiceProvider.GetRequiredService<IErrorReporter>();
            var guiInvoker = ServiceProvider.GetRequiredService<IGuiInvoker>();
            var viewModel = new EditorViewModel(callsign, downlinkMessageViewModels.ToArray(), mediator, errorReporter, guiInvoker);
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
        public bool HasActiveDownlinkMessages { get; set; }
        public bool HasSuspendedMessage { get; set; }
        public bool Unable { get; set; }
    }
}
