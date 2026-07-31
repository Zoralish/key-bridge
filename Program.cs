using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KeyBridge;
using Microsoft.AspNetCore.DataProtection;
using Spectre.Console;

Console.OutputEncoding = Encoding.UTF8;

ConsoleUI.Title();

KeyBridgeConfig config;
try
{
    config = KeyBridgeConfigManager.Load();
}
catch (FileNotFoundException ex)
{
    await SetupNewConfigFile(ex.Message);
    return;
}
catch (Exception ex) when (ex is JsonException or InvalidDataException)
{
    ConsoleUI.DisplayError("Configuration file could not be processed", ex);
    ResetAppData();
    return;
}

using var cts = CreateConsoleCancellationSource();
var Runner = new KeyBridgeRunner(config, cts.Token);

var action = ConsoleUI.DisplayMenu();
try
{
    switch (action)
    {
        case ConsoleUI.ActionCommand.OpenLocalDB:
            await ConsoleUI.RunWithSpinnerAsync(
                "Opening local database...",
                Runner.OpenLocalDatabaseAsync
            );
            break;
        case ConsoleUI.ActionCommand.SyncDBs:
            await ConsoleUI.RunWithSpinnerAsync(
                "Synchronizing...",
                Runner.SynchronizeDatabasesAsync
            );

            ConsoleUI.WriteLogMessage(ConsoleUI.SuccessTag, "Databases synchronized successfully");
            ConsoleUI.Shutdown(0);
            break;
        case ConsoleUI.ActionCommand.Reset:
            ResetAppData();
            break;
        case ConsoleUI.ActionCommand.Terminate:
            Environment.Exit(0);
            break;
        default:
            throw new UnreachableException();
    }
}
catch (Exception ex)
    when (ex
            is OperationCanceledException
                or IOException
                or InvalidOperationException
                or CryptographicException
                or InvalidDataException
    )
{
    ConsoleUI.DisplayError("Action could not performed", ex);
    ConsoleUI.Shutdown(1);
}

static async Task SetupNewConfigFile(string message)
{
    ConsoleUI.WriteLogMessage(ConsoleUI.ErrorTag, message);
    ConsoleUI.WriteLogMessage(ConsoleUI.InfoTag, "Configuration initialization");
    Console.WriteLine();

    string kpPath = PromptForValidPath("KeePass.exe", KeyBridgeConfig.ExecutableExtension);
    string kpScript = PromptForValidPath("KPScript.exe", KeyBridgeConfig.ExecutableExtension);
    string localDb = PromptForValidPath("Local database", KeyBridgeConfig.DatabaseExtension);
    string cloudDb = PromptForValidPath("Cloud database", KeyBridgeConfig.DatabaseExtension);
    string keyFile = PromptForValidPath("Key file", KeyBridgeConfig.KeyFileExtension);

    var prompt = new TextPrompt<string>(
        $"[{ConsoleUI.GeneralColorHex}]Enter your master password [{ConsoleUI.SelectionColorHex}]\u00bb[/][/]"
    ).Secret();
    string encryptedPassword = EncryptionService.Protector.Protect(AnsiConsole.Prompt(prompt));

    KeyBridgeConfig configData = new()
    {
        KeePassPath = kpPath,
        KPScriptPath = kpScript,
        LocalDatabasePath = localDb,
        CloudDatabsePath = cloudDb,
        KeyFilePath = keyFile,
        EncryptedPassword = encryptedPassword,
    };

    try
    {
        await KeyBridgeConfigManager.SetupNew(configData);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        ConsoleUI.DisplayError("Failed to create config file!", ex);
        ConsoleUI.Shutdown(1);
        return;
    }

    AnsiConsole.WriteLine();
    ConsoleUI.WriteLogMessage(ConsoleUI.SuccessTag, "Configuration file created");
    ConsoleUI.Shutdown(0);

    static string PromptForValidPath(string displayName, string expectedExtension)
    {
        var prompt = new TextPrompt<string>(
            $"[{ConsoleUI.GeneralColorHex}]{displayName} location path [grey](Drag & drop file here)[/] [{ConsoleUI.SelectionColorHex}]\u00bb[/][/]"
        );
        string rawInput = AnsiConsole.Prompt(prompt);
        var span = rawInput.AsSpan().Trim();
        if (span.StartsWith("&"))
            span = span[1..].Trim();
        var sanitizedPath = span.Trim("\"' ").ToString();

        if (!KeyBridgeConfigManager.IsValidEntry(sanitizedPath, expectedExtension))
        {
            ConsoleUI.DisplayError(
                "Invalid entry.",
                new InvalidDataException(
                    $"The file was not found or has an invalid extension. Expected extension: \"{expectedExtension}\"."
                )
            );
            ConsoleUI.Shutdown(1);
        }

        return sanitizedPath;
    }

    return;
}

static CancellationTokenSource CreateConsoleCancellationSource()
{
    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (sender, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    return cts;
}

static void ResetAppData()
{
    string question =
        $"[{ConsoleUI.GeneralColorHex}][{ConsoleUI.ErrorColorHex}]Delete[/] all configuration data?[/]";
    string hint = $"[{ConsoleUI.SelectionColorHex}]" + "[[y/N]]" + "[/]";

    var prompt = new TextPrompt<string>($"{question} {hint}").DefaultValue("N").HideDefaultValue();
    var response = AnsiConsole.Prompt(prompt);

    bool confirmed = char.ToUpperInvariant(response.Trim()[0]) == 'Y';
    if (!confirmed)
        ConsoleUI.Shutdown(0);

    try
    {
        DataDeletionServices.Delete();
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        ConsoleUI.DisplayError("Failed to delete existing configuration", ex);
        ConsoleUI.Shutdown(1);
    }

    AnsiConsole.WriteLine();
    ConsoleUI.WriteLogMessage(ConsoleUI.InfoTag, "Configuration file has been deleted");
    ConsoleUI.Shutdown(0);
}
