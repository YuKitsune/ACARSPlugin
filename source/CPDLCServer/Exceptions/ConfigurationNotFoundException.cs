namespace CPDLCServer.Exceptions;

public sealed class ConfigurationNotFoundException(string flightSimulationNetwork, string stationIdentifier) 
    : Exception($"No configuration found for {flightSimulationNetwork} on {stationIdentifier}");