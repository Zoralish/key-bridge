using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeyBridge;

public class KeyBridgeConfig
{
    public const string Executable = ".exe";
    public const string Database = ".kdbx";
    public const string KeyFile = ".keyx";
    public required string KeePassPath { get; init; }
    public required string KPScriptPath { get; init; }
    public required string LocalDatabasePath { get; init; }
    public required string CloudDatabsePath { get; init; }
    public required string KeyFilePath { get; init; }
    public required string EncryptedPassword { get; init; }

    [JsonIgnore]
    public string? DecryptedPassword { set; get; }
}

public class KeyBridgeConfigManager
{
    public static KeyBridgeConfig Load()
    {
        if (!File.Exists(AppPaths.ConfigPath))
            throw new FileNotFoundException("Configuration file not found");

        string json = File.ReadAllText(AppPaths.ConfigPath);

        var config =
            JsonSerializer.Deserialize<KeyBridgeConfig>(json)
            ?? throw new JsonException("Configuration file could not be processed");

        VerifyConfig(config);

        return config;
    }

    public static async Task SetupNew(KeyBridgeConfig configData)
    {
        string json = JsonSerializer.Serialize(
            configData,
            new JsonSerializerOptions { WriteIndented = true }
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

        if (!IsValidEntry(config.KeePassPath, KeyBridgeConfig.Executable))
            missingFiles.Add($"KeePass.exe: {config.KeePassPath}");

        if (!IsValidEntry(config.KPScriptPath, KeyBridgeConfig.Executable))
            missingFiles.Add($"KPScript.exe: {config.KPScriptPath}");

        if (!IsValidEntry(config.LocalDatabasePath, KeyBridgeConfig.Database))
            missingFiles.Add($"Database1.kbdx: {config.LocalDatabasePath}");

        if (!IsValidEntry(config.CloudDatabsePath, KeyBridgeConfig.Database))
            missingFiles.Add($"Database2.kbdx: {config.CloudDatabsePath}");

        if (!IsValidEntry(config.KeyFilePath, KeyBridgeConfig.KeyFile))
            missingFiles.Add($"Key.keyx: {config.KeyFilePath}");

        if (missingFiles.Count > 0)
            throw new InvalidDataException(
                $"The following required files could not be found:{Environment.NewLine}"
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
            conflictingPaths.Add($"KeePass.exe & KPScript.exe: {config.KeePassPath}");
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
                    + string.Join(Environment.NewLine, conflictingPaths)
            );
        }
    }
}
