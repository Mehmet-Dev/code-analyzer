using Spectre.Console;

namespace CodeAnalyzer.Helpers.ConsoleUI;

public static class ConsoleUI
{
    /// <summary>
    /// Wait for keyboard input before continuing
    /// </summary>
    /// <param name="message">Message to display</param>
    public static void WaitForKey(string message = "Press any key to continue...")
    {
        AnsiConsole.MarkupLine($"[grey]{message}[/]");
        Console.ReadKey(true);
    }

    /// <summary>
    /// Print a message in red color
    /// </summary>
    /// <param name="message">Message to display</param>
    public static void PrintError(string message)
        => AnsiConsole.MarkupLine($"[bold red]{message}[/]");
}