using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;

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

        var directoryFiles = Directory.EnumerateFiles(AppPaths.BackupFolderPath);

        if (directoryFiles.Count() > BackupHistorySize)
            throw new InvalidDataException(
                "The backup directory contains more backup files than expected."
            );
        if (directoryFiles.Count() < BackupHistorySize)
            return;

        Regex regex = BackupNameRegex();
        var backupFiles = new List<BackupFileInfo>();

        foreach (string filePath in directoryFiles)
        {
            string extension = Path.GetExtension(filePath);
            if (extension != ".zip")
                throw new InvalidDataException(
                    $"Unexpected backup file extension '{extension}'. Expected '.zip'."
                );

            string fileName = Path.GetFileNameWithoutExtension(filePath);
            if (!regex.IsMatch(fileName))
                throw new InvalidDataException(
                    $"The backup file name '{fileName}' does not match the expected naming convention."
                );

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
                    $"The backup file name '{fileName}' does not match the expected naming convention."
                );

            if (timestamp > DateTime.UtcNow.AddHours(1))
                throw new InvalidDataException("The backup file timestamp is in the future.");

            backupFiles.Add(new BackupFileInfo(filePath, timestamp));
        }

        string earliestBackupFile =
            (backupFiles.MinBy(b => b.Timestamp)?.Path) ?? throw new UnreachableException();

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

    [GeneratedRegex(@"^\d{8}_\d{6}")]
    private static partial Regex BackupNameRegex();
}
