namespace KeyBridge;

public static class AppPaths
{
    public static readonly string ConfigPath = Path.Combine(
        AppContext.BaseDirectory,
        "config.json"
    );
    public static readonly string KeyStoragePath = Path.Combine(
        AppContext.BaseDirectory,
        "CryptoKeys"
    );
}

public static class LogTags
{
    public static readonly string infoTag = "[cyan]" + "INFO".PadRight(7) + "[/]";
    public static readonly string errorTag = "[red]" + "ERROR".PadRight(7) + "[/]";
    public static readonly string successTag = "[green]" + "SUCCESS".PadRight(7) + "[/]";
}
