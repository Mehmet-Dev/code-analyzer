using CodeAnalyzer.Analyzers;
using CodeAnalyzer.Helpers.ConsoleUI;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Spectre.Console;

namespace CodeAnalyzer;

public class Analyzer
{
    private SyntaxNode _root;

    public Analyzer(SyntaxNode root)
        => _root = root;

    /// <summary>
    /// Go through each individual method and check how many lines it is
    /// </summary>
    /// <param name="methods">An Enumerator of MethodDeclarationSyntax (methods)</param>
    /// <param name="lengthTreshold">The line treshold</param>
    public void CheckMethodLengths(int lengthTreshold)
    {
        AnsiConsole.MarkupLine($"[bold yellow]Method Length Report[/]");

        Dictionary<string, int> methods = MethodLengthAnalyzer.Analyze(_root);

        foreach (var (name, lineCount) in methods)
        {
            if (lineCount > lengthTreshold)
            {
                AnsiConsole.MarkupLine($"[red]{name}[/] is [bold red]{lineCount}[/] lines long.");
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]{name}[/] is {lineCount} lines long.");
            }
        }

        ConsoleUI.WaitForKey();
    }

    /// <summary>
    /// Check the amount of parameters on every method
    /// </summary>
    /// <param name="methods">An Enumerator of Methods</param>
    public void CheckParameterCount()
    {
        AnsiConsole.MarkupLine($"[bold yellow]Parameter Count Report[/]");

        var parameterCounts = ParameterCountAnalzer.CheckParameterCount(_root);

        foreach (var counts in parameterCounts)
        {
            var (name, count) = counts.Key;
            var (message, color) = counts.Value;

            AnsiConsole.MarkupLine($"[{color}]{name} has {count} parameters. {message}[/]");
        }

        ConsoleUI.WaitForKey();
    }

    /// <summary>
    /// Check for magic numbers.
    /// Magic numbers are number literals that aren't assigned to values.
    /// </summary>
    public void CheckMagicNumbers()
    {
        var magicNumbers = MagicNumberAnalyzer.CheckMagicNumbers(_root);
        AnsiConsole.MarkupLine($"[bold yellow]Magic Number Detection[/]");

        foreach (var (name, message) in magicNumbers)
        {
            AnsiConsole.MarkupLine($"[italic blue]{name}[/]");
            AnsiConsole.MarkupLine(message);
        }
        AnsiConsole.MarkupLine($"\n[red italic]Always[/][red] remember to use a descriptive constant instead of a magic number.[/]");
        ConsoleUI.WaitForKey();
    }

    /// <summary>
    /// A simple TODO/FIXME-style comment detector.
    /// It has a general list to filter from.
    /// It displays at what line the comments are found.
    /// </summary>
    /// <param name="root">The root (compilation unit)</param>
    public void CheckPendingTasks()
    {
        List<string> comments = PendingTasksAnalyzer.CheckPendingTasks(_root);
        AnsiConsole.MarkupLine($"[bold yellow]FIXME and TODO comments[/]");

        if (comments.Count == 0)
        {
            AnsiConsole.MarkupLine("No TODO/FIXME-style comments found.");
        }

        foreach (string comment in comments)
        {
            AnsiConsole.MarkupLine(comment);
        }

        ConsoleUI.WaitForKey();
    }

    /// <summary>
    /// Check how complex your methods are.
    /// It checks for decision points (if, foreach, etc.)
    /// And uses an arbitrary value to determine how complex a method is.
    /// </summary>
    public void CheckMethodComplexity()
    {
        List<string> lines = ComplexityAnalyzer.CalculateComplexity(_root);

        AnsiConsole.MarkupLine($"[bold yellow]Method complexity[/]");

        foreach (string line in lines)
        {
            AnsiConsole.MarkupLine(line);
        }
        ConsoleUI.WaitForKey();
    }

    /// <summary>
    /// Shows general file stats like total lines, amount of methods, etc.
    /// </summary>
    public void ShowFileStats()
    {
        List<string> writes = FileAnalyzer.ShowFileStats(_root);

        foreach (string write in writes)
        {
            AnsiConsole.MarkupLine(write);
        }

        ConsoleUI.WaitForKey();
    }

    /// <summary>
    /// Display dead code information
    /// </summary>
    public void CheckDeadCode()
    {
        Dictionary<string, List<string>> methodWarnings = DeadCodeAnalyzer.DetermineDeadCode(_root);
        AnsiConsole.MarkupLine($"[bold yellow]Dead code detection[/]");

        if (methodWarnings.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]No dead code detected![/]");
            return;
        }

        foreach (var (method, warnings) in methodWarnings)
        {
            AnsiConsole.MarkupLine($"[blue]{method}[/]");

            foreach (string warning in warnings)
            {
                AnsiConsole.MarkupLine($"[red]- {warning}[/]");
            }
        }

        ConsoleUI.WaitForKey();
    }

    /// <summary>
    /// Get the root of the SyntaxTree
    /// </summary>
    /// <param name="filePath">The file path</param>
    /// <returns>A SyntaxNode (the root)</returns>
    public static SyntaxNode ReturnRoot(string filePath)
    {
        if (!filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("File contents are not valid C# code.");
        var code = File.ReadAllText(filePath);

        return CSharpSyntaxTree.ParseText(code).GetRoot();
    }
}