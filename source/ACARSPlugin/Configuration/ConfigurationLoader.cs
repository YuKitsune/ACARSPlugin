using System.IO;
using System.Reflection;
using System.Text.Json;

namespace ACARSPlugin.Configuration;

public static class ConfigurationLoader
{
    private const string ConfigFileName = "ACARS.json";

    public static AcarsConfiguration Load()
    {
        var configPath = GetConfigFilePath();
        
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Could not find configuration file at {configPath}");
        }

        var json = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<AcarsConfiguration>(json)!;
    }

    private static string GetConfigFilePath()
    {
        // Get the directory where the plugin assembly is located
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        if (assemblyDirectory == null)
            throw new InvalidOperationException("Could not determine assembly directory");

        return Path.Combine(assemblyDirectory, ConfigFileName);
    }
}
