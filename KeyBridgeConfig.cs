using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeyBridge;

public class KeyBridgeConfig
{
    public const string ExecutableExtension = ".exe";
    public const string DatabaseExtension = ".kdbx";
    public const string KeyFileExtension = ".keyx";
    public required string KeePassPath { get; init; }
    public required string KPScriptPath { get; init; }
    public required string LocalDatabasePath { get; init; }
    public required string CloudDatabsePath { get; init; }
    public required string KeyFilePath { get; init; }
    public required string EncryptedPassword { get; init; }
}

public class KeyBridgeConfigManager
{
    public static KeyBridgeConfig Load()
    {
        if (!File.Exists(AppPaths.ConfigPath))
            throw new FileNotFoundException("Configuration file not found");

        string json = File.ReadAllText(AppPaths.ConfigPath);

        var config =
            JsonSerializer.Deserialize(json, KeyBridgeJsonContext.Default.KeyBridgeConfig)
            ?? throw new JsonException("Configuration file could not be processed");

        VerifyConfig(config);

        return config;
    }

    public static async Task SetupNew(KeyBridgeConfig configData)
    {
        string json = JsonSerializer.Serialize(
            configData,
            KeyBridgeJsonContext.IndentedContext.KeyBridgeConfig
        );
        await File.WriteAllTextAsync(AppPaths.ConfigPath, json);
    }

    public static bool IsValidEntry(string filePath, string expectedExtension)
    {
        if (!File.Exists(filePath))
            return false;
        return string.Equals(
            Path.GetExtension(filePath),
            expectedExtension,
            StringComparison.OrdinalIgnoreCase
        );
    }

    private static void VerifyConfig(KeyBridgeConfig config)
    {
        var missingFiles = new List<string>();

        if (!IsValidEntry(config.KeePassPath, KeyBridgeConfig.ExecutableExtension))
            missingFiles.Add($"KeePass executable: {config.KeePassPath}");

        if (!IsValidEntry(config.KPScriptPath, KeyBridgeConfig.ExecutableExtension))
            missingFiles.Add($"KPScript executable: {config.KPScriptPath}");

        if (!IsValidEntry(config.LocalDatabasePath, KeyBridgeConfig.DatabaseExtension))
            missingFiles.Add($"Local database: {config.LocalDatabasePath}");

        if (!IsValidEntry(config.CloudDatabsePath, KeyBridgeConfig.DatabaseExtension))
            missingFiles.Add($"Cloud database: {config.CloudDatabsePath}");

        if (!IsValidEntry(config.KeyFilePath, KeyBridgeConfig.KeyFileExtension))
            missingFiles.Add($"Key file: {config.KeyFilePath}");

        if (missingFiles.Count > 0)
            throw new InvalidDataException(
                "The following required files could not be found:"
                    + Environment.NewLine
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, missingFiles)
            );

        var conflictingPaths = new List<string>();

        if (
            string.Equals(
                config.KeePassPath,
                config.KPScriptPath,
                StringComparison.OrdinalIgnoreCase
            )
        )
            conflictingPaths.Add($"KeePass & KPScript executables: {config.KeePassPath}");
        if (
            string.Equals(
                config.LocalDatabasePath,
                config.CloudDatabsePath,
                StringComparison.OrdinalIgnoreCase
            )
        )
            conflictingPaths.Add($"Local & Cloud databases: {config.LocalDatabasePath}");

        if (conflictingPaths.Count > 0)
        {
            throw new InvalidDataException(
                "The following paths collide:"
                    + Environment.NewLine
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, conflictingPaths)
            );
        }
    }
}

[JsonSerializable(typeof(KeyBridgeConfig))]
internal partial class KeyBridgeJsonContext : JsonSerializerContext
{
    public static KeyBridgeJsonContext IndentedContext { get; } =
        new(new JsonSerializerOptions { WriteIndented = true });
}
