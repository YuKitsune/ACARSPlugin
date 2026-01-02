using CPDLCServer.Clients;
using CPDLCServer.Exceptions;
using CPDLCServer.Infrastructure;
using CPDLCServer.Messages;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Services;

public class AircraftConnectionWatchdog : IHostedService
{
    readonly TimeSpan _interval = TimeSpan.FromMinutes(15);
#if DEBUG
    readonly TimeSpan _errorInterval = TimeSpan.FromSeconds(5);
#else
    readonly TimeSpan _errorInterval = TimeSpan.FromMinutes(5);
#endif
    
    readonly IClientManager _clientManager;
    readonly IAircraftRepository _aircraftRepository;
    readonly IClock _clock;
    readonly IMediator _mediator;
    readonly ILogger _logger;
    
    readonly AcarsConfiguration[] _acarsConfigurations;
    
    CancellationTokenSource? _cancellationTokenSource;
    Task? _task;

    public AircraftConnectionWatchdog(IClientManager clientManager, IAircraftRepository aircraftRepository, IClock clock, IMediator mediator, ILogger logger, IConfiguration configuration)
    {
        _clientManager = clientManager;
        _aircraftRepository = aircraftRepository;
        _clock = clock;
        _logger = logger;
        _mediator = mediator;

        // TODO: Copied from elsewhere. Need to standardise.
        var acarsConfigurationSection = configuration.GetSection("Acars");
        var configurationList = new List<AcarsConfiguration>();

        foreach (var section in acarsConfigurationSection.GetChildren())
        {
            var type = section["Type"];
            var config = type switch
            {
                "Hoppie" => section.Get<HoppiesConfiguration>(),
                _ => throw new NotSupportedException($"ACARS configuration type '{type}' is not supported")
            };

            if (config is not null)
            {
                configurationList.Add(config);
            }
        }

        _acarsConfigurations = configurationList.ToArray();
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_cancellationTokenSource is not null || _task is not null)
            throw new Exception("Already started");

        _cancellationTokenSource = new CancellationTokenSource();
        _task = DoWork(_cancellationTokenSource.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cancellationTokenSource is null || _task is null)
            throw new Exception("Already stopped");
        
        await _cancellationTokenSource.CancelAsync();
        await _task;
        
        _cancellationTokenSource = null;
        _task = null;
    }
    
    async Task DoWork(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var checkTimeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                checkTimeoutCancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));

                try
                {
                    _logger.Information("Checking aircraft connection activity");

                    foreach (var acarsConfiguration in _acarsConfigurations)
                    {
                        var client = await _clientManager.GetAcarsClient(
                            acarsConfiguration.FlightSimulationNetwork,
                            acarsConfiguration.StationIdentifier,
                            checkTimeoutCancellationTokenSource.Token);

                        var trackedConnections = await _aircraftRepository.All(
                            acarsConfiguration.FlightSimulationNetwork,
                            acarsConfiguration.StationIdentifier,
                            checkTimeoutCancellationTokenSource.Token);

                        var activeConnections = await client.ListConnections(checkTimeoutCancellationTokenSource.Token);
                        var lostConnections = trackedConnections.Where(t => !activeConnections.Contains(t.Callsign));

                        foreach (var aircraftConnection in lostConnections)
                        {
                            // If we've seen the aircraft recently, don't boot the connection
                            // Could be a temporary loss of connection
                            var timeSinceLastSeen = _clock.UtcNow() - aircraftConnection.LastSeen;
                            if (timeSinceLastSeen <= _interval)
                            {
                                continue;
                            }
                            
                            await _mediator.Publish(
                                new AircraftLost(
                                    aircraftConnection.FlightSimulationNetwork,
                                    aircraftConnection.StationId,
                                    aircraftConnection.Callsign),
                                checkTimeoutCancellationTokenSource.Token);
                        }
                    }

                    await Task.Delay(_interval, cancellationToken);
                }
                catch (OperationCanceledException) when (checkTimeoutCancellationTokenSource.IsCancellationRequested)
                {
                    // Timeout
                    _logger.Warning("Aircraft connection activity check timed out");
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Stopping
                }
                catch (ConfigurationNotFoundException)
                {
                    // Race condition: Client manager probably hasn't created the client yet
                    await Task.Delay(_errorInterval, cancellationToken);
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, "Error checking aircraft connection activity");
                    await Task.Delay(_errorInterval, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Stopping
            _logger.Information("Stopping aircraft connection watchdog...");
        }
        catch (Exception exception)
        {
            _logger.Fatal(exception, "Error checking aircraft connection activity");
        }
    }
}