using Microsoft.AspNetCore.DataProtection;

namespace KeyBridge;

public static class EncryptionService
{
    public static IDataProtector Protector { get; }

    static EncryptionService()
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
