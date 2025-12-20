using ACARSPlugin.Server.Contracts;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace ACARSPlugin.Server;

/// <summary>
/// Manages the SignalR connection to the ACARS server.
/// </summary>
public class SignalRConnectionManager(
    string serverEndpoint,
    string serverApiKey,
    IDownlinkHandlerDelegate downlinkHandlerDelegate)
    : IDisposable
{
    private const string AuthHeaderName = "X-ACARS-ApiKey";

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

    /// <summary>
    /// Initializes the SignalR connection.
    /// </summary>
    public async Task InitializeAsync(string stationId, string callsign)
    {
        if (_connection != null)
        {
            await DisposeConnectionAsync();
        }

        var url = $"{serverEndpoint}?network=VATSIM&stationId={stationId}&callsign={callsign}";

        _connection = new HubConnectionBuilder()
            .WithUrl(url, options =>
            {
                options.Headers.Add(AuthHeaderName, serverApiKey);
            })
            .AddJsonProtocol(options =>
            {
                // Configure JSON to handle polymorphic types
                options.PayloadSerializerOptions.TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver();
            })
            .WithAutomaticReconnect()
            .Build();

        RegisterHandlers();
        RegisterConnectionEvents();
    }

    /// <summary>
    /// Starts the connection to the server.
    /// </summary>
    public async Task StartAsync()
    {
        if (_connection == null)
        {
            throw new InvalidOperationException("Connection not initialized. Call InitializeAsync first.");
        }

        if (_connection.State == HubConnectionState.Connected)
        {
            return;
        }

        try
        {
            await _connection.StartAsync();
            OnConnectionStateChanged(HubConnectionState.Connected);
        }
        catch (Exception ex)
        {
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
            return;
        }

        try
        {
            await _connection.StopAsync();
            OnConnectionStateChanged(HubConnectionState.Disconnected);
        }
        catch (Exception ex)
        {
            OnConnectionError(ex);
            throw;
        }
    }

    void RegisterHandlers()
    {
        if (_connection == null) return;

        // Register handlers for concrete downlink types
        _connection.On<CpdlcDownlink>("DownlinkReceived", downlink =>
            WithCancellationToken<IDownlinkMessage>(downlinkHandlerDelegate.DownlinkReceived)(downlink));

        _connection.On<CpdlcDownlinkReply>("DownlinkReceived", downlink =>
            WithCancellationToken<IDownlinkMessage>(downlinkHandlerDelegate.DownlinkReceived)(downlink));

        _connection.On<TelexDownlink>("DownlinkReceived", downlink =>
            WithCancellationToken<IDownlinkMessage>(downlinkHandlerDelegate.DownlinkReceived)(downlink));
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
    public async Task SendUplink(IUplinkMessage uplinkMessage, CancellationToken cancellationToken)
    {
        EnsureConnected();
        if (uplinkMessage is ICpdlcReply)
        {
            await _connection!.InvokeAsync("SendUplinkReply", uplinkMessage, cancellationToken: cancellationToken);
        }
        else
        {
            await _connection!.InvokeAsync("SendUplink", uplinkMessage, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Registers connection lifecycle event handlers.
    /// </summary>
    private void RegisterConnectionEvents()
    {
        if (_connection == null) return;

        _connection.Closed += async error =>
        {
            OnConnectionStateChanged(HubConnectionState.Disconnected);
            if (error != null)
            {
                OnConnectionError(error);
            }

            await Task.CompletedTask;
        };

        _connection.Reconnecting += error =>
        {
            OnConnectionStateChanged(HubConnectionState.Reconnecting);
            if (error != null)
            {
                OnConnectionError(error);
            }

            return Task.CompletedTask;
        };

        _connection.Reconnected += connectionId =>
        {
            OnConnectionStateChanged(HubConnectionState.Connected);
            return Task.CompletedTask;
        };
    }

    private void OnConnectionStateChanged(HubConnectionState newState)
    {
        ConnectionStateChanged?.Invoke(this, newState);
    }

    private void OnConnectionError(Exception error)
    {
        ConnectionError?.Invoke(this, error);
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

        DisposeConnectionAsync().GetAwaiter().GetResult();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    private async Task DisposeConnectionAsync()
    {
        if (_connection != null)
        {
            try
            {
                await _connection.StopAsync();
            }
            catch
            {
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
