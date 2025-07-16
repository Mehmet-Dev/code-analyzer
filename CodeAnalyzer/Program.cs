using CodeAnalyzer.Helpers.UserInfo;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Spectre.Console;

namespace CodeAnalyzer;

class Program
{
    static void Main(string[] args)
    {
        Console.Clear();
        if (args.Length == 0)
        {
            Console.WriteLine("Please provide a C# file path to analyze.");
            return;
        }

        var filePath = args[0];

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"File '{filePath}' not found.");
            return;
        }

        int lengthTreshold = UserPrompt.AskComplexityLevel() switch
        {
            1 => 20,
            2 => 50,
            3 => 100,
            4 => 150,
            _ => 20
        };

        SyntaxNode root = null!;

        try
        {
            root = Analyzer.ReturnRoot(filePath);
        }
        catch (InvalidDataException e)
        {
            AnsiConsole.MarkupLine($"[bold red]{e.Message}[/]");
            Environment.Exit(1);
        }

        Analyzer analyzer = new(root);

        analyzer.CheckMethodLengths(lengthTreshold);
        Console.Clear();
        analyzer.CheckParameterCount();
        Console.Clear();
        analyzer.CheckMagicNumbers();
        Console.Clear();
        analyzer.CheckPendingTasks();
        Console.Clear();
        analyzer.ShowFileStats();
        Console.Clear();
        analyzer.CheckMethodComplexity();
    }
}