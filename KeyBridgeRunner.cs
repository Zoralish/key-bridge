using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace KeyBridge;

public class KeyBridgeRunner(KeyBridgeConfig config, CancellationToken cancellationToken)
{
    private readonly KeyBridgeConfig _config = config;
    private readonly CancellationToken _cancellationToken = cancellationToken;

    [DoesNotReturn]
    public async Task OpenLocalDatabase()
    {
        string[] syncArguments =
        [
            "-pw-stdin",
            _config.LocalDatabasePath,
            $"-keyfile:{_config.KeyFilePath}",
        ];

        ReadOnlyMemory<char> readOnlyDecryptedPassword = _config.DecryptedPassword.AsMemory();

        await ProcessRunner.RunExternalProcessAsync(
            _config.KeePassPath,
            syncArguments,
            awaitResults: false,
            _cancellationToken,
            readOnlyDecryptedPassword
        );

        Environment.Exit(0);
    }

    public async Task<(int exitCode, string message)?> SynchronizeDatabasesAsync()
    {
        string[] syncArguments =
        [
            "-c:Sync",
            "-keyprompt",
            _config.LocalDatabasePath,
            $"-File:{_config.CloudDatabsePath}",
        ];

        ReadOnlyMemory<char>[] writerArguments =
        [
            _config.DecryptedPassword.AsMemory(),
            _config.KeyFilePath.AsMemory(),
            ReadOnlyMemory<char>.Empty,
        ];

        var result =
            await ProcessRunner.RunExternalProcessAsync(
                _config.KPScriptPath,
                syncArguments,
                awaitResults: true,
                _cancellationToken,
                writerArguments
            ) ?? throw new UnreachableException();

        return result;
    }
}
