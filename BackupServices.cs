using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;

namespace KeyBridge;

public static partial class DataBackupServices
{
    private const string DateTimeFormat = "yyyyMMdd_HHmmss'Z'";
    private const int BackupHistorySize = 3;

    private record BackupFileInfo(string Path, DateTime Timestamp);

    public static void Backup(params string[] filePaths)
    {
        Prune();
        CreateBackupFile(filePaths);
    }

    private static void Prune()
    {
        if (!Directory.Exists(AppPaths.BackupFolderPath))
        {
            Directory.CreateDirectory(AppPaths.BackupFolderPath);
            return;
        }

        var directoryFiles = Directory.GetFiles(AppPaths.BackupFolderPath);

        if (directoryFiles.Length > BackupHistorySize)
            throw new InvalidDataException(
                "The backup directory contains more backup files than expected."
            );
        if (directoryFiles.Length < BackupHistorySize)
            return;

        var validatedBackups = new List<BackupFileInfo>();

        foreach (string filePath in directoryFiles)
        {
            string extension = Path.GetExtension(filePath);
            if (extension != ".zip")
                throw new InvalidDataException(
                    $"Unexpected backup file extension '{extension}'. Expected '.zip'."
                );

            string fileName = Path.GetFileNameWithoutExtension(filePath);

            if (
                !DateTime.TryParseExact(
                    fileName,
                    DateTimeFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out DateTime timestamp
                )
            )
                throw new InvalidDataException(
                    $"The backup file name '{fileName}' does not match the expected format '{DateTimeFormat}'."
                );

            if (timestamp > DateTime.UtcNow.AddHours(1))
                throw new InvalidDataException("The backup file timestamp is in the future.");

            validatedBackups.Add(new BackupFileInfo(filePath, timestamp));
        }

        string earliestBackupFile =
            (validatedBackups.MinBy(b => b.Timestamp)?.Path) ?? throw new UnreachableException();

        File.Delete(earliestBackupFile);
    }

    private static void CreateBackupFile(params string[] filePaths)
    {
        string zipPath = Path.Combine(
            AppPaths.BackupFolderPath,
            DateTime.UtcNow.ToString(DateTimeFormat) + ".zip"
        );

        using ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        foreach (string filePath in filePaths)
        {
            string entryName = Path.GetFileName(filePath);
            archive.CreateEntryFromFile(filePath, entryName);
        }
    }
}
