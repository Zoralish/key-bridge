using System.ComponentModel;
using System.Diagnostics;

namespace KeyBridge;

public static class ProcessRunner
{
    public static async Task RunExternalProcessAsync(
        string processName,
        string[] syncArguments,
        bool awaitExit,
        CancellationToken cancellationToken,
        params ReadOnlyMemory<char>[] writerArguments
    )
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = processName,
            RedirectStandardInput = true,
            RedirectStandardOutput = awaitExit,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var argument in syncArguments)
            startInfo.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
                throw new InvalidOperationException("Failed to start the process.");
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException(
                $"OS failed to execute the process. Details:{Environment.NewLine}{ex.Message}",
                ex
            );
        }

        Task<string>? outputTask = null;
        if (awaitExit)
            outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);

        try
        {
            using StreamWriter writer = process.StandardInput;
            foreach (var argument in writerArguments)
            {
                await writer.WriteLineAsync(argument, cancellationToken);
            }
            await writer.FlushAsync(cancellationToken);
        }
        catch (IOException)
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            throw;
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        if (!awaitExit)
            return;

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        if (process.ExitCode != 0)
        {
            if (outputTask is null)
                throw new UnreachableException();

            string output = (await outputTask).Trim();
            throw new InvalidOperationException(output);
        }

        return;
    }
}
