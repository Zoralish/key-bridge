using System.ComponentModel;
using System.Diagnostics;

namespace KeyBridge;

public static class ProcessRunner
{
    public static async Task<(int exitCode, string message)?> RunExternalProcessAsync(
        string processName,
        string[] syncArguments,
        bool awaitResults,
        CancellationToken cancellationToken,
        params ReadOnlyMemory<char>[] writerArguments
    )
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = processName,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
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
                $"OS failed to execute process. Details: {ex.Message}",
                ex
            );
        }

        Task<string> outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);

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
                throw;
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException(
                $"The {processName} process timed out while sending credentials."
            );
        }

        if (!awaitResults)
            return null;

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"The {processName} execution was cancelled.");
        }

        string output = (await outputTask).Trim();

        return (process.ExitCode, output);
    }
}
