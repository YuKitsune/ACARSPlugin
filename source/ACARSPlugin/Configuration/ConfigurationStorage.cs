using Microsoft.Win32;

namespace ACARSPlugin.Configuration;

public static class ConfigurationStorage
{
    private const string RegistryKeyPath = @"Software\ACARSPlugin";
    private const string ApiKeyValueName = "ApiKey";

    public static void SaveApiKey(string apiKey)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
        key.SetValue(ApiKeyValueName, apiKey);
    }

    public static string LoadApiKey()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
        return key?.GetValue(ApiKeyValueName) as string ?? string.Empty;
    }
}
