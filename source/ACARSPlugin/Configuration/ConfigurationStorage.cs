using Microsoft.Win32;

namespace ACARSPlugin.Configuration;

/// <summary>
/// Handles storing and retrieving plugin configuration from the Windows Registry.
/// </summary>
public static class ConfigurationStorage
{
    private const string RegistryKeyPath = @"Software\ACARSPlugin";
    private const string ServerEndpointValueName = "ServerEndpoint";
    private const string ApiKeyValueName = "ApiKey";
    private const string StationIdentifierValueName = "StationIdentifier";

    /// <summary>
    /// Saves the configuration to the Windows Registry.
    /// </summary>
    public static void Save(string serverEndpoint, string apiKey, string stationIdentifier)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath, writable: true);
        if (key is null)
        {
            throw new InvalidOperationException($"Failed to create or open registry key: {RegistryKeyPath}");
        }

        key.SetValue(ServerEndpointValueName, serverEndpoint, RegistryValueKind.String);
        key.SetValue(ApiKeyValueName, apiKey, RegistryValueKind.String);
        key.SetValue(StationIdentifierValueName, stationIdentifier, RegistryValueKind.String);
    }

    /// <summary>
    /// Loads the configuration from the Windows Registry.
    /// </summary>
    /// <returns>A tuple containing the server endpoint, API key, and station identifier. Returns null values if not found.</returns>
    public static (string? ServerEndpoint, string? ApiKey, string? StationIdentifier) Load()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: false);
        if (key is null)
        {
            return (null, null, null);
        }

        var serverEndpoint = key.GetValue(ServerEndpointValueName) as string;
        var apiKey = key.GetValue(ApiKeyValueName) as string;
        var stationIdentifier = key.GetValue(StationIdentifierValueName) as string;

        return (serverEndpoint, apiKey, stationIdentifier);
    }
}
