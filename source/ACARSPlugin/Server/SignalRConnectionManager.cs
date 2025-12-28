using ACARSPlugin.Server.Contracts;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ACARSPlugin.Server;

/// <summary>
/// Manages the SignalR connection to the ACARS server.
/// </summary>
public class SignalRConnectionManager(
    string serverEndpoint,
    IDownlinkHandlerDelegate downlinkHandlerDelegate,
    ILogger logger)
    : IDisposable
{
    private HubConnection? _connection;
    private bool _isDisposed;

    /// <summary>
    /// Gets the current connection state.
    /// </summary>
    public HubConnectionState ConnectionState => _connection?.State ?? HubConnectionState.Disconnected;

    /// <summary>
    /// Gets whether the connection is currently active.
    /// </summary>
    public bool IsConnected => ConnectionState == HubConnectionState.Connected;

    /// <summary>
    /// Event raised when the connection state changes.
    /// </summary>
    public event EventHandler<HubConnectionState>? ConnectionStateChanged;

    /// <summary>
    /// Event raised when a connection error occurs.
    /// </summary>
    public event EventHandler<Exception>? ConnectionError;

    public string StationIdentifier { get; private set; } = string.Empty;

    /// <summary>
    /// Initializes the SignalR connection.
    /// </summary>
    public async Task InitializeAsync(string stationId, string callsign)
    {
        logger.Information("Initializing SignalR connection for station {StationId} with callsign {Callsign}", stationId, callsign);

        if (_connection != null)
        {
            logger.Debug("Disposing existing connection before re-initialization");
            await DisposeConnectionAsync();
        }

        var url = $"{serverEndpoint}?network=VATSIM&stationId={stationId}&callsign={callsign}";
        logger.Debug("Building SignalR connection to {Url}", url);

        var hubConnectionBuilder = new HubConnectionBuilder()
            .WithUrl(url, options =>
            {
#if DEBUG
                // In DEBUG mode, bypass SSL certificate validation for local development
                options.HttpMessageHandlerFactory = _ =>
                {
                    var handler = new System.Net.Http.HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                    };
                    return handler;
                };
#endif
            })
            .AddJsonProtocol(options =>
            {
                // Configure JSON to handle polymorphic types
                options.PayloadSerializerOptions.TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver();
            })
            .WithAutomaticReconnect();

        _connection = hubConnectionBuilder.Build();

        RegisterHandlers();
        RegisterConnectionEvents();

        StationIdentifier = stationId;
        logger.Information("SignalR connection initialized successfully");
    }

    /// <summary>
    /// Starts the connection to the server.
    /// </summary>
    public async Task StartAsync()
    {
        if (_connection == null)
        {
            logger.Error("Cannot start connection: not initialized");
            throw new InvalidOperationException("Connection not initialized. Call InitializeAsync first.");
        }

        if (_connection.State == HubConnectionState.Connected)
        {
            logger.Debug("Connection already active, skipping start");
            return;
        }

        try
        {
            logger.Information("Starting SignalR connection to server");
            await _connection.StartAsync();
            OnConnectionStateChanged(HubConnectionState.Connected);
            logger.Information("SignalR connection started successfully");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to start SignalR connection");
            OnConnectionError(ex);
            throw;
        }
    }

    /// <summary>
    /// Stops the connection to the server.
    /// </summary>
    public async Task StopAsync()
    {
        if (_connection == null || _connection.State == HubConnectionState.Disconnected)
        {
            logger.Debug("Connection already stopped or not initialized, skipping stop");
            return;
        }

        try
        {
            logger.Information("Stopping SignalR connection");
            await _connection.StopAsync();
            OnConnectionStateChanged(HubConnectionState.Disconnected);

            StationIdentifier = string.Empty;
            logger.Information("SignalR connection stopped successfully");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error while stopping SignalR connection");
            OnConnectionError(ex);
            throw;
        }
    }

    void RegisterHandlers()
    {
        if (_connection == null) return;

        logger.Debug("Registering SignalR message handlers");
        _connection.On<CpdlcDownlink>("DownlinkReceived", downlink =>
            WithCancellationToken<IDownlinkMessage>(downlinkHandlerDelegate.DownlinkReceived)(downlink));
        _connection.On<ConnectedAircraftInfo>("AircraftConnected", connectedAircraftInfo =>
            WithCancellationToken<ConnectedAircraftInfo>(downlinkHandlerDelegate.AircraftConnected)(connectedAircraftInfo));
        _connection.On<string>("AircraftDisconnected", callsign =>
            WithCancellationToken<string>(downlinkHandlerDelegate.AircraftDisconnected)(callsign));
    }

    Func<T, Task> WithCancellationToken<T>(Func<T, CancellationToken, Task> action)
    {
        // TODO: Make configurable
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(10));

        return x => action(x, cancellationTokenSource.Token);
    }

    /// <summary>
    /// Sends a generic message to the server.
    /// </summary>
    public async Task<CpdlcUplink> SendUplink(
        string recipient,
        int? replyToDownlinkId,
        CpdlcUplinkResponseType responseType,
        string content,
        CancellationToken cancellationToken)
    {
        logger.Debug("Sending uplink to {Recipient} (ReplyTo: {ReplyToDownlinkId}, Type: {ResponseType})",
            recipient, replyToDownlinkId, responseType);

        EnsureConnected();
        var result = await _connection!.InvokeAsync<SendUplinkResult>(
            "SendUplink",
            recipient,
            replyToDownlinkId,
            responseType, content,
            cancellationToken: cancellationToken);

        logger.Information("Uplink sent successfully to {Recipient} with ID {UplinkId}",
            recipient, result.UplinkMessage.Id);

        return result.UplinkMessage;
    }
    
    public record SendUplinkResult(CpdlcUplink UplinkMessage);

    public async Task<ConnectedAircraftInfo[]> GetConnectedAircraft(CancellationToken cancellationToken)
    {
        logger.Debug("Requesting connected aircraft from server");

        EnsureConnected();
        var result = await _connection!.InvokeAsync<GetConnectedAircraftResult>(
            "GetConnectedAircraft",
            cancellationToken);

        logger.Debug("Received {AircraftCount} connected aircraft from server", result.Aircraft.Length);

        return result.Aircraft;
    }
    
    record GetConnectedAircraftResult(ConnectedAircraftInfo[] Aircraft);

    /// <summary>
    /// Registers connection lifecycle event handlers.
    /// </summary>
    private void RegisterConnectionEvents()
    {
        if (_connection == null) return;

        logger.Debug("Registering SignalR connection lifecycle event handlers");

        _connection.Closed += async error =>
        {
            if (error != null)
            {
                logger.Warning(error, "SignalR connection closed with error");
                OnConnectionError(error);
            }
            else
            {
                logger.Information("SignalR connection closed");
            }

            OnConnectionStateChanged(HubConnectionState.Disconnected);
            await Task.CompletedTask;
        };

        _connection.Reconnecting += error =>
        {
            if (error != null)
            {
                logger.Warning(error, "SignalR connection lost, attempting to reconnect");
                OnConnectionError(error);
            }
            else
            {
                logger.Information("SignalR connection reconnecting");
            }

            OnConnectionStateChanged(HubConnectionState.Reconnecting);
            return Task.CompletedTask;
        };

        _connection.Reconnected += connectionId =>
        {
            logger.Information("SignalR connection reconnected successfully (ConnectionId: {ConnectionId})", connectionId);
            OnConnectionStateChanged(HubConnectionState.Connected);
            return Task.CompletedTask;
        };
    }

    private void OnConnectionStateChanged(HubConnectionState newState)
    {
        logger.Debug("Connection state changed to {State}", newState);
        ConnectionStateChanged?.Invoke(this, newState);
    }

    private void OnConnectionError(Exception error)
    {
        logger.Error(error, "SignalR connection error occurred");
        ConnectionError?.Invoke(this, error);
        downlinkHandlerDelegate.Error(error);
    }

    /// <summary>
    /// Ensures the connection is active before attempting operations.
    /// </summary>
    private void EnsureConnected()
    {
        if (_connection == null)
        {
            throw new InvalidOperationException("Connection not initialized. Call InitializeAsync first.");
        }

        if (_connection.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException($"Connection is not active. Current state: {_connection.State}");
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        logger.Debug("Disposing SignalR connection manager");
        DisposeConnectionAsync().GetAwaiter().GetResult();
        _isDisposed = true;
        logger.Debug("SignalR connection manager disposed");
        GC.SuppressFinalize(this);
    }

    private async Task DisposeConnectionAsync()
    {
        if (_connection != null)
        {
            try
            {
                logger.Debug("Stopping connection during disposal");
                await _connection.StopAsync();
            }
            catch (Exception ex)
            {
                logger.Debug(ex, "Error stopping connection during disposal (ignoring)");
                // Ignore errors during disposal
            }
            finally
            {
                await _connection.DisposeAsync();
                _connection = null;
            }
        }
    }
}
