using Spectre.Console;

namespace CodeAnalyzer.Helpers.ConsoleUI;

public static class ConsoleUI
{
    public static void WaitForKey(string message = "Press any key to continue...")
    {
        AnsiConsole.MarkupLine($"[grey]{message}[/]");
        Console.ReadKey(true);
    }
}