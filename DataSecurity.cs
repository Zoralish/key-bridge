using Microsoft.AspNetCore.DataProtection;

namespace KeyBridge;

public static class DataProtection
{
    public static IDataProtector Protector { get; }

    static DataProtection()
    {
        Directory.CreateDirectory(AppPaths.KeyStoragePath);
        Protector = DataProtectionProvider
            .Create(
                new DirectoryInfo(AppPaths.KeyStoragePath),
                builder => builder.ProtectKeysWithDpapi()
            )
            .CreateProtector("KeyBridge.PasswordManager", "MasterPassword.v1");
    }
}

public static class DataPurge
{
    public static void PurgeEverything()
    {
        if (File.Exists(AppPaths.ConfigPath))
            File.Delete(AppPaths.ConfigPath);
        if (Directory.Exists(AppPaths.KeyStoragePath))
            Directory.Delete(AppPaths.KeyStoragePath, recursive: true);
    }
}
