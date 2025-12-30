using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
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
using Serilog;
using vatsys;
using vatsys.Plugin;

namespace ACARSPlugin;

// TODO: Text message fallback
// TODO: window frame styling
// TODO: Complex variable entry (popups and validation)
// TODO: Fix jurisdiction checks
// TODO: ADS-C
// TODO: Strip items

[Export(typeof(IPlugin))]
public class Plugin : ILabelPlugin, IRecipient<CurrentMessagesChanged>, IRecipient<ConnectedAircraftChanged>
{
#if DEBUG
    public const string Name = "ACARS Plugin - Debug";
#else
    public const string Name = "ACARS Plugin";
#endif

    static readonly Dictionary<string, DateTimeOffset> ErrorMessages = new();

    // Cache for CustomStripOrLabelItem to avoid expensive lookups on every label update
    readonly ConcurrentDictionary<string, CustomStripOrLabelItem> _labelItemCache = new();

    readonly Channel<Func<Task>> _workQueue = Channel.CreateUnbounded<Func<Task>>();
    readonly Task _worker;

    // Cached theme colors to avoid deadlock when accessing from non-GUI threads
    CustomColour _cachedDownlinkColor = new(0, 105, 0);
    CustomColour _cachedUnableDownlinkColor = new(230, 127, 127);
    CustomColour _cachedSuspendedColor = new(255, 255, 255);

    string IPlugin.Name => Name;

    IServiceProvider ServiceProvider { get; set; }

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

            _worker = Worker(CancellationToken.None);
        }
        catch (Exception ex)
        {
            AddError(ex);
        }
    }

    async Task Worker(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var work = await _workQueue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                await work().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
                break;
            }
            catch (Exception ex)
            {
                try
                {
                    AddError(ex);
                }
                catch
                {
                    // Ignore errors during error reporting
                }
            }
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

        var logger = ConfigureLogger(acarsConfiguration);

        ServiceProvider = new ServiceCollection()
            .AddSingleton(logger)
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

    ILogger ConfigureLogger(AcarsConfiguration configuration)
    {
        var logFileName = Path.Combine(Helpers.GetFilesFolder(), "cpdlc_log.txt");

        var logger = new LoggerConfiguration()
            .WriteTo.File(
                path: logFileName,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: configuration.MaxLogFileAgeDays,
                outputTemplate: "{Timestamp:u} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .MinimumLevel.Is(configuration.LogLevel)
            .CreateLogger();

        Log.Logger = logger;

        return logger;
    }

    void NetworkConnected(object sender, EventArgs e)
    {
        Log.Information("Connected to VATSIM as {Callsign}", Network.Callsign);

        // TODO: Connect if auto-connect enabled
    }

    void NetworkDisconnected(object sender, EventArgs e)
    {
        Log.Information("Disconnected from VATSIM");

        try
        {
            var mediator =  ServiceProvider.GetService<IMediator>();
            if (mediator is null)
                return;

            _workQueue.Writer.TryWrite(async () => await mediator.Send(new DisconnectRequest()).ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            AddError(ex);
        }
    }

    public static void AddError(Exception exception)
    {
        TryAddErrorInternal(exception);
        Log.Error(exception, "An error has occurred");
    }

    public static void AddError(Exception exception, string message)
    {
        TryAddErrorInternal(exception);
        Log.Error(exception, message);
    }

    static void TryAddErrorInternal(Exception exception)
    {
        // Don't flood the error window with the same message over and over again
        if (ErrorMessages.TryGetValue(exception.Message, out var lastShown) &&
            DateTimeOffset.Now - lastShown <= TimeSpan.FromMinutes(1))
            return;

        Errors.Add(exception, Name);
        ErrorMessages.Add(exception.Message, DateTimeOffset.Now);
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

    public static bool ShouldDisplayMessage(Dialogue dialogue)
    {
        var fdr = FDP2.GetFDRs.FirstOrDefault(f => f.Callsign == dialogue.AircraftCallsign);
        if (fdr == null)
            return false;

        return ShouldDisplayMessage(dialogue, fdr);
    }

    static bool ShouldDisplayMessage(Dialogue dialogue, FDP2.FDR fdr)
    {
        // If we have jurisdiction, show the message
        if (fdr.IsTrackedByMe)
        {
            return true;
        }

        // VATSIM-ISM: If the dialogue was with us, then show the messages
        if (dialogue.ControllerCallsign == Network.Callsign)
        {
            return true;
        }

        // TODO: If nobody has jurisdiction, and we're the next owner, show the message
        // TODO: If nobody has jurisdiction, and we were the last owner, show the message

        // Fallback: If nobody has jurisdiction, show the message to everyone
        if (!fdr.IsTracked)
        {
            return true;
        }

        return false;
    }

    public CustomLabelItem? GetCustomLabelItem(
        string itemType,
        Track track,
        FDP2.FDR flightDataRecord,
        RDP.RadarTrack radarTrack)
    {
        try
        {
            if (itemType.StartsWith("ACARSPLUGIN_TEXTSTATUS"))
            {
                return GetTextStatusLabelItem(itemType, flightDataRecord);
            }

            if (itemType.StartsWith("ACARSPLUGIN_CPDLCSTATUS"))
            {
                return GetCpdlcStatusLabelItem(itemType, flightDataRecord);
            }
        }
        catch (Exception ex)
        {
            AddError(ex, "Failed to generate custom label item");
        }

        return null;
    }

    CustomLabelItem? GetTextStatusLabelItem(string itemType, FDP2.FDR? flightDataRecord)
    {
        // vatSys bug: Custom background colours can't be drawn selectively.
        // vatSys won't draw the custom background if the original colour (specified in the Labels.xml file) is transparent (or empty).
        // To work around this, we define two label items. One with the background, and one without.
        // If we need to draw a custom background colour, we return `null` for the one without the background.

        if (flightDataRecord is null)
            return null;

        var lastTextMessage = Network.GetRadioMessages
            .LastOrDefault(r => r.Address == flightDataRecord.Callsign && !r.Acknowledged);

        string? text = null;
        CustomColour? backgroundColour = null;

        if (flightDataRecord.TextOnly)
        {
            text = "T";
        }
        else if (flightDataRecord.ReceiveOnly)
        {
            text = "R";
        }
        else if (lastTextMessage is not null)
        {
            // Only show "V" when there is an unacknowledged message
            text = "V";
            backgroundColour = _cachedDownlinkColor;
        }

        // Don't take up the space if it's not necessary
        if (text is null)
            return null;

        // vatSys bug: custom background colours can't be drawn selectively.
        // To work around this, we define two label items. One with the background, and one without.
        if (backgroundColour is not null && itemType != "ACARSPLUGIN_TEXTSTATUS_BG")
            return null;

        if (backgroundColour is null && itemType != "ACARSPLUGIN_TEXTSTATUS")
            return null;

        var textLabelItem = new CustomLabelItem
        {
            Type = itemType,
            Text = text,
            Border = BorderFlags.All
        };

        if (backgroundColour is not null)
        {
            textLabelItem.BackColourIdentity = Colours.Identities.Custom;
            textLabelItem.CustomBackColour = backgroundColour;
        }

        // Left-click to open the CPDLC Menu
        textLabelItem.OnMouseClick = args =>
        {
            if (args.Button != CustomLabelItemMouseButton.Left)
                return;

            if (lastTextMessage is not null)
            {
                MMI.OpenCPDLCMenu(lastTextMessage);
            }
            else
            {
                MMI.OpenCPDLCWindow(flightDataRecord);
            }

            args.Handled = true;
        };

        return textLabelItem;
    }

    CustomLabelItem? GetCpdlcStatusLabelItem(string itemType, FDP2.FDR flightDataRecord)
    {
        // vatSys bug: Custom background colours can't be drawn selectively.
        // vatSys won't draw the custom background if the original colour (specified in the Labels.xml file) is transparent (or empty).
        // To work around this, we define two label items. One with the background, and one without.
        // If we need to draw a custom background colour, we return `null` for the one without the background.

        // Blank by default
        var labelItem = new CustomLabelItem
        {
            Type = itemType,
            Text = " "
        };

        var customItem = GetCustomStripOrLabelItem(flightDataRecord);
        if (customItem is null)
        {
            if (itemType == "ACARSPLUGIN_CPDLCSTATUS_BG")
                return null;

            return labelItem;
        }

        labelItem.Text = customItem.Text;
        if (customItem.BackgroundColour is not null && itemType != "ACARSPLUGIN_CPDLCSTATUS_BG")
            return null;

        if (customItem.BackgroundColour is null && itemType != "ACARSPLUGIN_CPDLCSTATUS")
            return null;

        if (customItem.BackgroundColour is not null)
        {
            labelItem.BackColourIdentity = Colours.Identities.Custom;
            labelItem.CustomBackColour = customItem.BackgroundColour;
        }

        labelItem.OnMouseClick = args =>
        {
            if (args.Button != CustomLabelItemMouseButton.Left)
                return;

            customItem.LeftClickCallback();
            args.Handled = true;
        };

        return labelItem;
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
            AddError(ex);
            return null;
        }
    }

    record CustomStripOrLabelItem(
        string Text,
        CustomColour? BackgroundColour,
        Action LeftClickCallback);

    public CustomColour? SelectASDTrackColour(Track track) => null;

    public CustomColour? SelectGroundTrackColour(Track track) => null;

    async Task RebuildLabelItemCache(CancellationToken cancellationToken = default)
    {
        try
        {
            var allExistingKeys = _labelItemCache.Keys;
            var allUpdatedKeys = new List<string>();

            _labelItemCache.Clear();

            var repository = ServiceProvider.GetRequiredService<MessageRepository>();
            var aircraftTracker = ServiceProvider.GetRequiredService<AircraftConnectionTracker>();

            var connectedAircraft = await aircraftTracker.GetConnectedAircraft(cancellationToken).ConfigureAwait(false);

            foreach (var flightDataRecord in FDP2.GetFDRs)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                if (flightDataRecord is null)
                    continue;

                var connection = connectedAircraft.FirstOrDefault(c => c.Callsign == flightDataRecord.Callsign);

                var openDialogues = await repository.GetCurrentDialogues().ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                    return;

                var hasOpenDownlinkMessages = openDialogues
                    .SelectMany(d => d.Messages)
                    .OfType<DownlinkMessage>()
                    .Any(m => !m.IsClosed);

                // TODO: Check if they're connected to the ACARS network. Equipment flags are unreliable.
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

                var unacknowledgedUnableReceived = openDialogues
                    .SelectMany(d => d.Messages)
                    .OfType<DownlinkMessage>()
                    .Any(m => m.Sender == flightDataRecord.Callsign && m.Content.Contains("UNABLE") && !m.IsAcknowledged);

                // TODO: Suspended messages
                var hasSuspendedMessage = false;

                var guiInvoker = ServiceProvider.GetRequiredService<IGuiInvoker>();

                var text = " ";
                CustomColour? backgroundColour = null;
                CustomColour? foregroundColour = null;
                Action leftClickAction = () => { };

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
                            AddError(ex, "Error opening CPDLC Window");
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
                            AddError(ex, "Error opening CPDLC window");
                        }
                    };

                    // Color only changes for the responsible controller
                    if (flightDataRecord.IsTrackedByMe)
                    {
                        if (unacknowledgedUnableReceived)
                        {
                            backgroundColour = _cachedUnableDownlinkColor;
                        }
                        else if (hasOpenDownlinkMessages)
                        {
                            backgroundColour = _cachedDownlinkColor;
                        }
                    }
                }

                _labelItemCache[flightDataRecord.Callsign] = new CustomStripOrLabelItem(
                    text,
                    backgroundColour,
                    leftClickAction);

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
            AddError(ex);
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

                var selectedCallsign = MMI.SelectedTrack?.GetFDR()?.Callsign;

                var viewModel = new HistoryViewModel(
                    configuration,
                    mediator,
                    guiInvoker,
                    errorReporter,
                    selectedCallsign);

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

    void FDP2OnFDRsChanged(object sender, FDP2.FDRsChangedEventArgs e)
    {
        try
        {
            _workQueue.Writer.TryWrite(() => RebuildLabelItemCache());
        }
        catch (Exception ex)
        {
            AddError(ex);
        }
    }

    public void Receive(ConnectedAircraftChanged _)
    {
        try
        {
            _workQueue.Writer.TryWrite(() => RebuildLabelItemCache());
        }
        catch (Exception ex)
        {
            AddError(ex);
        }
    }

    public void Receive(CurrentMessagesChanged _)
    {
        try
        {
            _workQueue.Writer.TryWrite(async () =>
            {
                await RebuildLabelItemCache().ConfigureAwait(false);

                var mediator = ServiceProvider.GetRequiredService<IMediator>();
                var windowManager = ServiceProvider.GetRequiredService<WindowManager>();

                // Use the filtered GetCurrentDialogues request which only returns dialogues for the current controller
                var response = await mediator.Send(new GetCurrentDialoguesRequest()).ConfigureAwait(false);

                var guiInvoker = ServiceProvider.GetRequiredService<IGuiInvoker>();

                if (response.Dialogues.Any())
                {
                    guiInvoker.InvokeOnGUI(_ => OpenCurrentMessagesWindow());
                }
                else
                {
                    windowManager.TryRemoveWindow(WindowKeys.CurrentMessages);
                }
            });
        }
        catch (Exception ex)
        {
            AddError(ex);
        }
    }

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
                var configuration = ServiceProvider.GetRequiredService<AcarsConfiguration>();
                var mediator = ServiceProvider.GetRequiredService<IMediator>();

                var response = mediator.Send(new GetCurrentDialoguesRequest(), cancellationToken)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                var downlinkMessageViewModels = new List<DownlinkMessageViewModel>();

                foreach (var dialogue in response.Dialogues)
                {
                    if (dialogue.AircraftCallsign != callsign)
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
                    configuration,
                    mediator,
                    errorReporter,
                    guiInvoker,
                    windowHandle);

                var control = new EditorWindow(viewModel);
                return control;
            });
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
            AddError(ex);
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
            AddError(ex);
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
