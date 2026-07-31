namespace KeyBridge;

public static class DataDeletionServices
{
    public static void Delete()
    {
        if (File.Exists(AppPaths.ConfigPath))
            File.Delete(AppPaths.ConfigPath);

        if (Directory.Exists(AppPaths.DataProtectionKeyRingPath))
            Directory.Delete(AppPaths.DataProtectionKeyRingPath, recursive: true);

        if (Directory.Exists(AppPaths.BackupFolderPath))
            Directory.Delete(AppPaths.BackupFolderPath, recursive: true);
    }
}
