namespace CPDLCServer.Exceptions;

public sealed class ConfigurationNotFoundException(string acarsClientId)
    : Exception($"No ACARS configuration found for client ID '{acarsClientId}'");