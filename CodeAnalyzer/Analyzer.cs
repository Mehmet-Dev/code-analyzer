using System;
using System.Linq;
using System.Runtime.CompilerServices;
using CodeAnalyzer.Helpers.ConsoleUI;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Spectre.Console;

namespace CodeAnalyzer;

public static class Analyzer
{
    /// <summary>
    /// Go through each individual method and check how many lines it is
    /// </summary>
    /// <param name="methods">An Enumerator of MethodDeclarationSyntax (methods)</param>
    /// <param name="lengthTreshold">The line treshold</param>
    public static void CheckMethodLengths(IEnumerable<MethodDeclarationSyntax> methods, int lengthTreshold)
    {
        AnsiConsole.MarkupLine($"[bold yellow]Method Length Report[/]");

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
    }

    /// <summary>
    /// Check the amount of parameters on every method
    /// </summary>
    /// <param name="methods">An Enumerator of Methods</param>
    public static void CheckParameterCount(IEnumerable<MethodDeclarationSyntax> methods)
    {
        AnsiConsole.MarkupLine($"[bold yellow]Parameter Count Report[/]");

        foreach (var method in methods)
        {
            IEnumerable<ParameterSyntax> parameters = method.ParameterList.Parameters;
            int count = parameters.Count();

            var (message, color) = ParameterCountString(count);

            AnsiConsole.MarkupLine($"[{color}]{method.Identifier.Text} has {count} parameters. {message}[/]");
        }

        ConsoleUI.WaitForKey();
    }

    public static void CheckMagicNumbers(IEnumerable<MethodDeclarationSyntax> methods)
    {
        HashSet<SyntaxKind> kindsToIgnore = new()
        {
            SyntaxKind.EnumMemberDeclaration,
            SyntaxKind.EnumDeclaration,
            SyntaxKind.AttributeArgument,
            SyntaxKind.Parameter,
            SyntaxKind.CaseSwitchLabel,
            SyntaxKind.UsingDirective,
            SyntaxKind.VariableDeclarator,
            SyntaxKind.CompilationUnit, // <- this is unnecessary but just a sanity check
            SyntaxKind.SwitchSection,
        };

        List<int> filterOut = [0, 1];
        AnsiConsole.MarkupLine($"[bold yellow]Magic Number Detection[/]");

        foreach (var method in methods)
        {
            bool magicNumberFound = false;
            AnsiConsole.MarkupLine($"[italic blue]{method.Identifier.Text}[/]");
            IEnumerable<LiteralExpressionSyntax> numerics = method.DescendantNodes()
                            .OfType<LiteralExpressionSyntax>()
                            .Where(lit => lit.IsKind(SyntaxKind.NumericLiteralExpression)); // Filter out everything that isn't a literal number

            foreach (LiteralExpressionSyntax numeric in numerics)
            {
                var value = numeric.Token.Value;
                if (value is int intValue)
                {
                    if (filterOut.Contains(intValue)) // if the token is that of 0 or 1 (usually true/false) skip
                        continue;
                }

                IEnumerable<SyntaxNode> ancestors = numeric.Ancestors().Take(5); // Limit to 5 at most as to not traverse way too high into the compilation unit
                IEnumerable<SyntaxKind> kinds = ancestors.Select(a => a.Kind());

                if (kinds.Any(a => kindsToIgnore.Contains(a))) // If the kind is that of the ones to ignore, skip the loop
                    continue;

                var ancestor = ancestors.FirstOrDefault(a =>
                a.IsKind(SyntaxKind.FieldDeclaration) ||
                a.IsKind(SyntaxKind.LocalDeclarationStatement)); // Grab a kind that's either a field or local declaration

                if (ancestor != null && IsConstDeclaration(ancestor)) // If it has a const modifier, skip this
                {
                    continue;
                }

                // After all the checks give an error
                var lineSpan = numeric.SyntaxTree.GetLineSpan(numeric.Span);
                int lineNumber = lineSpan.StartLinePosition.Line + 1;

                AnsiConsole.MarkupLine($"[red]- Magic number {numeric.Token.Value} found on line {lineNumber}[/]");
                magicNumberFound = true;
            }

            if (!magicNumberFound)
                AnsiConsole.MarkupLine("[green]- No magic number found[/]");
        }
        AnsiConsole.MarkupLine($"\n[red italic]Always[/][red] remember to use a descriptive constant instead of a magic number.[/]");
    }

    /// <summary>
    /// A simple TODO/FIXME-style comment detector.
    /// It has a general list to filter from.
    /// It displays at what line the comments are found.
    /// </summary>
    /// <param name="root">The root (compilation unit)</param>
    public static void CheckUnchangedCode(SyntaxNode root)
    {
        bool foundAny = false;
        List<string> todoMarkers = new()
        {
            "todo",
            "fixme",
            "hack",
            "xxx",       // often used as a "this needs attention" tag
            "bug",
            "note",      // sometimes used for reminders
            "tbd",       // to be decided
            "fix",       // shorthand some people use
            "optimize",  // performance-related TODOs
            "cleanup"    // used for code refactoring notes
        };

        var comments = root.DescendantTrivia()
                        .Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia));

        AnsiConsole.MarkupLine($"FIXME and TODO comments");

        foreach (SyntaxTrivia comment in comments)
        {
            string text = comment.ToString().ToLower();

            string? match = todoMarkers.FirstOrDefault(m =>
                text.Contains(m, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                foundAny = true;
                var lineNumber = comment.SyntaxTree.GetLineSpan(comment.Span).StartLinePosition.Line + 1;

                if (text.Trim().Length < 10)
                    AnsiConsole.MarkupLine($"[yellow]- {match.ToUpper()} on line {lineNumber} is vague, consider adding more detail.[/]");
                else
                    AnsiConsole.MarkupLine($"[red]- {match.ToUpper()} found on line {lineNumber}: [/][italic green]{text.Trim()}[/]");
            }
        }
        if (foundAny)
            AnsiConsole.MarkupLine("\n[blue]It shouldn't be difficult to fix them.[/]");
        else
            AnsiConsole.MarkupLine("\nNo TODO/FIXME-style comments found.");
    }

    /// <summary>
    /// Get the root of the SyntaxTree
    /// </summary>
    /// <param name="filePath">The file path</param>
    /// <returns>A SyntaxNode (the root)</returns>
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

    private static bool IsConstDeclaration(SyntaxNode node)
    {
        return node switch
        {
            FieldDeclarationSyntax field => field.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)),
            LocalDeclarationStatementSyntax local => local.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)),
            _ => false
        };
    }
}