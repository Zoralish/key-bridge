namespace KeyBridge;

public static class AppPaths
{
    public static readonly string ConfigPath = Path.Combine(
        AppContext.BaseDirectory,
        "keybridge-config.json"
    );
    public static readonly string DataProtectionKeyRingPath = Path.Combine(
        AppContext.BaseDirectory,
        "DataProtectionKeys"
    );
}
