using CodeAnalyzer.Helpers.ConsoleUI;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Spectre.Console;

namespace CodeAnalyzer;

public static class Analyzer
{
    private static int _phase = 1;

    public static void CheckMethodLengths(IEnumerable<MethodDeclarationSyntax> methods, int lengthTreshold)
    {
        AnsiConsole.MarkupLine($"[bold yellow]Phase {_phase} - Method Length Report[/]");

        foreach (var method in methods)
        {
            var lineSpan = method.SyntaxTree.GetLineSpan(method.Span);
            var lineCount = lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line + 1;

            if (lineCount > lengthTreshold)
            {
                AnsiConsole.MarkupLine($"[red]{method.Identifier.Text}[/] is [bold red]{lineCount}[/] lines long.");
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]{method.Identifier.Text}[/] is {lineCount} lines long.");
            }
        }

        ConsoleUI.WaitForKey();
        _phase++;
    }

    public static void CheckParameterCount(IEnumerable<MethodDeclarationSyntax> methods)
    {
        AnsiConsole.MarkupLine($"[bold yellow]Phase {_phase} - Parameter Count Report[/]");

        foreach (var method in methods)
        {
            IEnumerable<ParameterSyntax> parameters = method.ParameterList.Parameters;
            int count = parameters.Count();

            var (message, color) = ParameterCountString(count);

            AnsiConsole.MarkupLine($"[{color}]{method.Identifier.Text} has {count} parameters. {message}[/]");
        }

        ConsoleUI.WaitForKey();
        _phase++;
    }

    public static SyntaxNode ReturnRoot(string filePath)
    {
        var code = File.ReadAllText(filePath);

        return CSharpSyntaxTree.ParseText(code).GetRoot();
    }

    private static (string message, string color) ParameterCountString(int count)
    {
        if (count >= 7)
            return ("Excessive parameters; strongly reconsider redesigning the method.", "red");
        else if (count >= 5)
            return ("High parameter count; refactoring is recommended.", "orange");
        else if (count >= 3)
            return ("Moderate parameter count; consider simplifying if possible unless necessary.", "yellow");
        else
            return ("", "green");
    }

}