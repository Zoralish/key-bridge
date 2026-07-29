using System.Diagnostics.CodeAnalysis;
using Spectre.Console;

namespace KeyBridge;

public static class ConsoleUI
{
    public static readonly string infoTag = "[#7390B0]" + "INFO".PadRight(7) + "[/]";
    public static readonly string errorTag = "[#C15E67]" + "ERROR".PadRight(7) + "[/]";
    public static readonly string successTag = "[#6CA285]" + "SUCCESS".PadRight(7) + "[/]";

    public static readonly string GeneralColorHex = "#D2D8E1";
    public static readonly string HighlightColorHex = "#7E9CB9";
    public static readonly string SelectionColorHex = "#84BCA3";
    public static readonly string errorColorHex = "#D98A94";

    public enum ActionCommand
    {
        Unknown,
        OpenLocalDB,
        SyncDBs,
        Reset,
        Terminate,
    }

    record Choice
    {
        public Choice(ActionCommand value, string display)
        {
            string displayColor = (value is ActionCommand.Unknown) ? "#5C6777" : GeneralColorHex;
            Display = $"[{displayColor}]{display}[/]";

            Value = value;
        }

        public ActionCommand Value { get; }
        public string Display { get; }
    }

    public static void Title()
    {
        AnsiConsole.Clear();
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold #B83973]KeyBridge - CLI Utililty[/]");
        AnsiConsole.WriteLine();
    }

    public static void WriteLogMessage(string tag, string message)
    {
        AnsiConsole.MarkupLine($"{tag} [{GeneralColorHex}]{message}[/]");
    }

    public static ActionCommand DisplayMenu()
    {
        var action = AnsiConsole.Prompt(
            new SelectionPrompt<Choice>()
                .Title($"[{GeneralColorHex}]Select your [{HighlightColorHex}]action[/][/]")
                .UseConverter(c => c.Display)
                .HighlightStyle(Style.Parse(SelectionColorHex))
                .AddChoiceGroup(
                    new Choice(default, "Databases"),
                    [
                        new Choice(ActionCommand.OpenLocalDB, "Open local database"),
                        new Choice(ActionCommand.SyncDBs, "Synchozize databases"),
                    ]
                )
                .AddChoiceGroup(
                    new Choice(default, "Settings"),
                    [new Choice(ActionCommand.Reset, "Hard reset")]
                )
                .AddChoiceGroup(
                    new Choice(default, "General"),
                    [new Choice(ActionCommand.Terminate, "Exit")]
                )
        );

        return action.Value;
    }

    public static Task RunWithSpinnerAsync(string message, Func<Task> action)
    {
        return AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Dots3)
            .SpinnerStyle(Style.Parse(SelectionColorHex))
            .StartAsync(
                $"[{GeneralColorHex}]{message} [{HighlightColorHex}](Press Ctrl+C to cancel)[/][/]",
                async _ => await action()
            );
    }

    public static void DisplayError(string message, Exception ex)
    {
        Title();
        AnsiConsole.MarkupLine($"{errorTag} [{GeneralColorHex}]{message.EscapeMarkup()}[/]");

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold #D98A94]" + Markup.Remove(ex.Message) + "[/]");
        AnsiConsole.WriteLine();
    }

    [DoesNotReturn]
    public static void Shutdown(int exitCode)
    {
        AnsiConsole.Markup("[gray]Press any key to exit...[/]");
        AnsiConsole.Console.Input.ReadKey(intercept: true);
        Environment.Exit(exitCode);
    }
}
