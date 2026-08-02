namespace KeyBridge;

public class KeyBridgeRunner(KeyBridgeConfig config, CancellationToken cancellationToken)
{
    private readonly KeyBridgeConfig _config = config;
    private readonly CancellationToken _cancellationToken = cancellationToken;

    public async Task OpenLocalDatabaseAsync()
    {
        await RequestHydrationAsync(_config.LocalDatabasePath);

        string[] syncArguments =
        [
            "-pw-stdin",
            _config.LocalDatabasePath,
            $"-keyfile:{_config.KeyFilePath}",
        ];

        ReadOnlyMemory<char> readOnlyDecryptedPassword = EncryptionService
            .Decrypt(_config.EncryptedPassword)
            .AsMemory();

        await ProcessRunner.RunExternalProcessAsync(
            _config.KeePassPath,
            syncArguments,
            awaitExit: false,
            _cancellationToken,
            readOnlyDecryptedPassword
        );
    }

    public async Task SynchronizeDatabasesAsync()
    {
        await RequestHydrationAsync(_config.LocalDatabasePath);
        await RequestHydrationAsync(_config.CloudDatabsePath);

        DataBackupServices.Backup(_config.LocalDatabasePath, _config.CloudDatabsePath);

        string[] syncArguments =
        [
            "-c:Sync",
            "-keyprompt",
            _config.LocalDatabasePath,
            $"-File:{_config.CloudDatabsePath}",
        ];

        ReadOnlyMemory<char>[] writerArguments =
        [
            EncryptionService.Decrypt(_config.EncryptedPassword).AsMemory(),
            _config.KeyFilePath.AsMemory(),
            ReadOnlyMemory<char>.Empty,
        ];

        await ProcessRunner.RunExternalProcessAsync(
            _config.KPScriptPath,
            syncArguments,
            awaitExit: true,
            _cancellationToken,
            writerArguments
        );
    }

    private async Task RequestHydrationAsync(string path)
    {
        using var stream = new FileStream(
            path,
            new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.ReadWrite,
                Options = FileOptions.Asynchronous,
            }
        );

        await stream.CopyToAsync(Stream.Null, _cancellationToken);
    }
}
