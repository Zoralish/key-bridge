using Microsoft.AspNetCore.DataProtection;

namespace KeyBridge;

public static class MasterPasswordEncryption
{
    public static IDataProtector Protector { get; }

    static MasterPasswordEncryption()
    {
        Directory.CreateDirectory(AppPaths.DataProtectionKeyRingPath);
        Protector = DataProtectionProvider
            .Create(
                new DirectoryInfo(AppPaths.DataProtectionKeyRingPath),
                builder => builder.ProtectKeysWithDpapi()
            )
            .CreateProtector("KeyBridge.PasswordManager", "MasterPassword", "v1");
    }

    public static string Decrypt(string encryptedPassword)
    {
        return Protector.Unprotect(encryptedPassword);
    }
}

public static class AppDataReset
{
    public static void Reset()
    {
        if (File.Exists(AppPaths.ConfigPath))
            File.Delete(AppPaths.ConfigPath);

        if (Directory.Exists(AppPaths.DataProtectionKeyRingPath))
            Directory.Delete(AppPaths.DataProtectionKeyRingPath, recursive: true);
    }
}
