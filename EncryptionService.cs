using Microsoft.AspNetCore.DataProtection;

namespace KeyBridge;

public sealed class EncryptionService
{
    private readonly IDataProtector _protector;

    public EncryptionService()
    {
        Directory.CreateDirectory(AppPaths.DataProtectionKeyRingPath);
        _protector = DataProtectionProvider
            .Create(
                new DirectoryInfo(AppPaths.DataProtectionKeyRingPath),
                builder => builder.ProtectKeysWithDpapi()
            )
            .CreateProtector("KeyBridge.PasswordManager", "MasterPassword", "v1");
    }

    public string Encrypt(string password)
    {
        return _protector.Protect(password);
    }

    public string Decrypt(string encryptedPassword)
    {
        return _protector.Unprotect(encryptedPassword);
    }
}
