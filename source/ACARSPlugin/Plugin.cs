using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows;
using System.Windows.Forms;
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
using Microsoft.Win32;
using vatsys;
using vatsys.Plugin;

namespace ACARSPlugin;

// TODO: Jurisdiction checks
// TODO: Revise CPDLC message set (custom config)
// TODO: vatSys window
// TODO: Text fallback
// TODO: RELEASE

// TODO: Complex variable entry (popups and validation)
// TODO: ADS-C
// TODO: Strip items

[Export(typeof(IPlugin))]
public class Plugin : ILabelPlugin, IRecipient<CurrentMessagesChanged>, IRecipient<ConnectedAircraftChanged>, IDisposable
{
#if DEBUG
    public const string Name = "ACARS Plugin - Debug";
#else
    public const string Name = "ACARS Plugin";
#endif

    // Cache for CustomStripOrLabelItem to avoid expensive lookups on every label update
    private readonly ConcurrentDictionary<string, CustomStripOrLabelItem> _labelItemCache = new();

    private bool _isDisposed;
    private readonly SemaphoreSlim _disposeLock = new(1, 1);
    
    string IPlugin.Name => Name;

    public IServiceProvider ServiceProvider { get; private set; }

    public SignalRConnectionManager? ConnectionManager { get; set; }

    public Plugin()
    {
        try
        {
            EnsureDpiAwareness();

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
            .AddSingleton<WindowManager>()
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
            if (_isDisposed)
                return;

            var mediator =  ServiceProvider.GetService<IMediator>();
            if (mediator is null)
                return;

            await mediator.Send(new DisconnectRequest());
        }
        catch (Exception ex)
        {
            if (!_isDisposed)
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
    
    public CustomLabelItem? GetCustomLabelItem(string itemType, Track track, FDP2.FDR flightDataRecord, RDP.RadarTrack radarTrack)
    {
        var labelItem = new CustomLabelItem
        {
            Type = itemType,
            Text = " "
        };
        
        try
        {
            if (!itemType.StartsWith("ACARSPLUGIN_CPDLCSTATUS"))
                return labelItem;
            
            var customItem = GetCustomStripOrLabelItem(flightDataRecord);
            if (customItem is null)
            {
                if (itemType == "ACARSPLUGIN_CPDLCSTATUS_BG")
                    return null;
                
                return labelItem;
            }

            labelItem.Text = customItem.Text;

            // vatSys bug: custom background colours can't be drawn selectively.
            // vatSys won't draw the custom background if the original colour (specified in the Labels.xml file) is transparent (or empty).
            // To work around this, we define two label items. One with the background, and one without.
            // If we need to draw a custom background colour, we return `null` for the one without the background
            if (customItem.BackgroundColour is not null && itemType != "ACARSPLUGIN_CPDLCSTATUS_BG")
            {
                return null;
            }
            
            if (customItem.BackgroundColour is null && itemType != "ACARSPLUGIN_CPDLCSTATUS")
            {
                return null;
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

    CustomStripOrLabelItem? GetCustomStripOrLabelItem(FDP2.FDR flightDataRecord)
    {
        try
        {
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
        Action LeftClickCallback,
        Action MiddleClickCallback,
        Action RightClickCallback);

    public CustomColour? SelectASDTrackColour(Track track) => null;

    public CustomColour? SelectGroundTrackColour(Track track) => null;

    async Task RebuildLabelItemCache()
    {
        try
        {
            if (_isDisposed)
                return;

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

                var activeDialogues = await repository.GetCurrentDialogues();

                var hasActiveDownlinkMessages = activeDialogues.Any();
                
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

                var unacknowledgedUnableReceived = activeDialogues.SelectMany(d => d.Messages)
                    .OfType<DownlinkMessage>()
                    .Any(m => m.Content.Contains("UNABLE") && !m.IsAcknowledged);

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
                            guiInvoker.InvokeOnGUI(_ =>
                            {
                                OpenCpdlcWindow(flightDataRecord.Callsign, CancellationToken.None);
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
                            guiInvoker.InvokeOnGUI(_ =>
                            {
                                OpenCpdlcWindow(flightDataRecord.Callsign, CancellationToken.None);
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
                        if (hasActiveDownlinkMessages && unacknowledgedUnableReceived)
                        {
                            backgroundColour = _cachedUnableDownlinkColor;
                        }
                        else if (hasActiveDownlinkMessages)
                        {
                            backgroundColour = _cachedDownlinkColor;
                        }
                    }
                }

                _labelItemCache[flightDataRecord.Callsign] = new CustomStripOrLabelItem(
                    text,
                    backgroundColour,
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

    void OpenSetupWindow()
    {
        var windowManager = ServiceProvider.GetRequiredService<WindowManager>();

        windowManager.FocusOrCreateWindow(
            WindowKeys.Setup,
            windowHandle =>
            {
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

                var control = new SetupWindow(viewModel);
                return control;
            });
    }

    void OpenHistoryWindow()
    {
        var windowManager = ServiceProvider.GetRequiredService<WindowManager>();

        windowManager.FocusOrCreateWindow(
            WindowKeys.History,
            windowHandle =>
            {
                var configuration = ServiceProvider.GetRequiredService<AcarsConfiguration>();
                var mediator = ServiceProvider.GetRequiredService<IMediator>();
                var guiInvoker = ServiceProvider.GetRequiredService<IGuiInvoker>();
                var errorReporter = ServiceProvider.GetRequiredService<IErrorReporter>();

                var viewModel = new HistoryViewModel(
                    configuration,
                    mediator,
                    guiInvoker,
                    errorReporter);

                return new HistoryWindow(viewModel);
            });
    }

    void OpenCurrentMessagesWindow()
    {
        var windowManager = ServiceProvider.GetRequiredService<WindowManager>();

        windowManager.FocusOrCreateWindow(
            WindowKeys.CurrentMessages,
            windowHandle =>
            {
                var configuration = ServiceProvider.GetRequiredService<AcarsConfiguration>();
                var mediator = ServiceProvider.GetRequiredService<IMediator>();
                var guiInvoker = ServiceProvider.GetRequiredService<IGuiInvoker>();
                var errorReporter = ServiceProvider.GetRequiredService<IErrorReporter>();

                var viewModel = new CurrentMessagesViewModel(
                    configuration,
                    mediator,
                    guiInvoker,
                    errorReporter);

                return new CurrentMessagesWindow(viewModel);
            });
    }

    private async void FDP2OnFDRsChanged(object sender, FDP2.FDRsChangedEventArgs e)
    {
        try
        {
            if (_isDisposed)
                return;

            await RebuildLabelItemCache();
        }
        catch (Exception ex)
        {
            if (!_isDisposed)
                Errors.Add(ex, Name);
        }
    }

    public async void Receive(ConnectedAircraftChanged _)
    {
        try
        {
            if (_isDisposed)
                return;

            await RebuildLabelItemCache();
        }
        catch (Exception ex)
        {
            if (!_isDisposed)
                Errors.Add(ex, Name);
        }
    }
    
    public async void Receive(CurrentMessagesChanged _)
    {
        try
        {
            if (_isDisposed)
                return;

            await RebuildLabelItemCache();

            if (_isDisposed)
                return;

            var repository = ServiceProvider.GetRequiredService<MessageRepository>();
            var windowManager = ServiceProvider.GetRequiredService<WindowManager>();
            var currentDialogues = await repository.GetCurrentDialogues();

            if (_isDisposed)
                return;

            var guiInvoker = ServiceProvider.GetRequiredService<IGuiInvoker>();

            if (currentDialogues.Any())
            {
                guiInvoker.InvokeOnGUI(_ => OpenCurrentMessagesWindow());
            }
            else
            {
                windowManager.TryRemoveWindow(WindowKeys.CurrentMessages);
            }
        }
        catch (Exception ex)
        {
            if (!_isDisposed)
                Errors.Add(ex, Name);
        }
    }

    // Cached theme colors to avoid deadlock when accessing from non-GUI threads
    CustomColour _cachedDownlinkColor = new(0, 105, 0);
    CustomColour _cachedUnableDownlinkColor = new(230, 127, 127);
    CustomColour _cachedSuspendedColor = new(255, 255, 255);
    
    void OpenCpdlcWindow(string callsign, CancellationToken cancellationToken)
    {
        var windowManager = ServiceProvider.GetRequiredService<WindowManager>();

        // Close any existing editor window before opening a new one
        // Each editor is specific to a callsign, so we always want a fresh window
        windowManager.TryRemoveWindow(WindowKeys.Editor);

        windowManager.FocusOrCreateWindow(
            WindowKeys.Editor,
            windowHandle =>
            {
                var mediator = ServiceProvider.GetRequiredService<IMediator>();

                var response = mediator.Send(new GetCurrentDialoguesRequest(), cancellationToken)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                var downlinkMessageViewModels = new List<DownlinkMessageViewModel>();

                foreach (var dialogue in response.Dialogues)
                {
                    if (dialogue.Callsign != callsign)
                        continue;
                    
                    foreach (var message in dialogue.Messages)
                    {
                        if (message is not DownlinkMessage downlinkMessage || downlinkMessage.IsClosed || downlinkMessage.ResponseType == CpdlcDownlinkResponseType.NoResponse)
                            continue;

                        var downlinkMessageViewModel = new DownlinkMessageViewModel(
                            downlinkMessage,
                            standbySent: dialogue.HasStandbyResponse(downlinkMessage.Id),
                            deferred: dialogue.HasDeferredResponse(downlinkMessage.Id));

                        downlinkMessageViewModels.Add(downlinkMessageViewModel);
                    }
                }

                var errorReporter = ServiceProvider.GetRequiredService<IErrorReporter>();
                var guiInvoker = ServiceProvider.GetRequiredService<IGuiInvoker>();

                var viewModel = new EditorViewModel(
                    callsign,
                    downlinkMessageViewModels.ToArray(),
                    mediator,
                    errorReporter,
                    guiInvoker,
                    windowHandle);

                var control = new EditorWindow(viewModel);
                return control;
            });
    }

    public void Dispose()
    {
        _disposeLock.Wait();
        try
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            // Unregister event handlers
            Network.Connected -= NetworkConnected;
            Network.Disconnected -= NetworkDisconnected;
            FDP2.FDRsChanged -= FDP2OnFDRsChanged;
            WeakReferenceMessenger.Default.Unregister<CurrentMessagesChanged>(this);
            WeakReferenceMessenger.Default.Unregister<ConnectedAircraftChanged>(this);

            // Dispose MessageMonitorService
            var monitorService = ServiceProvider.GetService<MessageMonitorService>();
            monitorService?.DisposeAsync().AsTask().GetAwaiter().GetResult();

            // Dispose connection manager
            ConnectionManager?.Dispose();

            // Dispose service provider if it's disposable
            if (ServiceProvider is IDisposable disposableProvider)
                disposableProvider.Dispose();
        }
        catch (Exception ex)
        {
            // Log but don't throw during disposal
            try
            {
                Errors.Add(ex, Name);
            }
            catch
            {
                // Ignore errors during error reporting in disposal
            }
        }
        finally
        {
            _disposeLock.Release();
            _disposeLock.Dispose();
        }
    }

    void EnsureDpiAwareness()
    {
        try
        {
            if (!TryGetVatSysExecutablePath(out var vatSysExecutablePath))
                return;

            const string registryPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers";
            const string dpiValue = "DPIUNAWARE";

            using var key = Registry.CurrentUser.OpenSubKey(registryPath, writable: false);
            var existingValue = key?.GetValue(vatSysExecutablePath) as string;

            // If already set, exit early
            if (existingValue != null && existingValue.Contains(dpiValue))
                return;

            // Set the registry key
            using var writableKey = Registry.CurrentUser.OpenSubKey(registryPath, writable: true)
                ?? Registry.CurrentUser.CreateSubKey(registryPath);

            writableKey.SetValue(vatSysExecutablePath, dpiValue, RegistryValueKind.String);

            // Restart vatSys to apply the DPI setting
            RestartVatSys();
        }
        catch (Exception ex)
        {
            Errors.Add(ex, Name);
        }
    }

    void RestartVatSys()
    {
        try
        {
            if (!TryGetVatSysExecutablePath(out var vatSysExecutablePath))
                return;

            System.Diagnostics.Process.Start(vatSysExecutablePath);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Errors.Add(ex, Name);
        }
    }

    bool TryGetVatSysInstallationPath(out string? installationPath)
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Sawbe\vatSys");
        installationPath = key?.GetValue("Path") as string;
        return !string.IsNullOrEmpty(installationPath) && Directory.Exists(installationPath);
    }

    bool TryGetVatSysExecutablePath(out string? executablePath)
    {
        try
        {
            if (!TryGetVatSysInstallationPath(out var installationPath))
            {
                executablePath = null;
                return false;
            }

            executablePath = Path.Combine(installationPath, "bin", "vatSys.exe");
            return File.Exists(executablePath);
        }
        catch
        {
            executablePath = null;
            return false;
        }
    }
}
