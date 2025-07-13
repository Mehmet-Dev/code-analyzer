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
        ConsoleUI.WaitForKey();
    }

    /// <summary>
    /// A simple TODO/FIXME-style comment detector.
    /// It has a general list to filter from.
    /// It displays at what line the comments are found.
    /// </summary>
    /// <param name="root">The root (compilation unit)</param>
    public static int CheckUnchangedCode(SyntaxNode root, bool show = true)
    {
        int amount = 0;
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

        IEnumerable<SyntaxTrivia> comments = root.DescendantTrivia()
                        .Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia));
        if(show)
            AnsiConsole.MarkupLine($"FIXME and TODO comments");

        foreach (SyntaxTrivia comment in comments)
        {
            string text = comment.ToString().ToLower();

            string? match = todoMarkers.FirstOrDefault(m =>
                text.Contains(m, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                amount++;
                foundAny = true;
                var lineNumber = comment.SyntaxTree.GetLineSpan(comment.Span).StartLinePosition.Line + 1;

                if (show)
                {
                    if (text.Trim().Length < 10)
                        AnsiConsole.MarkupLine($"[yellow]- {match.ToUpper()} on line {lineNumber} is vague, consider adding more detail.[/]");
                    else
                        AnsiConsole.MarkupLine($"[red]- {match.ToUpper()} found on line {lineNumber}: [/][italic green]{text.Trim()}[/]");
                }
            }
        }

        if (show)
        {
            if (foundAny)
                AnsiConsole.MarkupLine("\n[blue]It shouldn't be difficult to fix them.[/]");
            else
                AnsiConsole.MarkupLine("\nNo TODO/FIXME-style comments found.");
        }

        if(show)
            ConsoleUI.WaitForKey();

        return amount;
    }

    public static void ShowFileStats(SyntaxNode root)
    {
        SyntaxTree tree = root.SyntaxTree;
        // Part 1: file line count
        int fullLength = tree.GetLineSpan(root.Span).EndLinePosition.Line + 1;

        //Part 2: Number of methods
        IEnumerable<MethodDeclarationSyntax> methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
        int amountOfMethods = methods.Count();

        // Part 3: Longest method by line count & average method length
        var (methodName, lineCount, average) = GetLongestMethodAndAverageMethodLength(methods);

        // Part 4: Number of classes
        int amountOfClasses = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Count();

        // Part 5: Number of TODO/FIXME comments
        int amountOfPendingTasks = CheckUnchangedCode(root, false);

        // Part 6: Number of properties/fields
        var (properties, fields) = GetAmountOfPropertiesAndFields(root);

        // Part 7: Calculate comment density
        double density = CalculateCommentDensity(root);

        AnsiConsole.MarkupLine("[bold yellow]File wide stats[/]");
        AnsiConsole.MarkupLine($"[green]Total lines:[/] {fullLength}");
        AnsiConsole.MarkupLine($"[green]Number of methods:[/] {amountOfMethods}");
        AnsiConsole.MarkupLine($"[green]Longest method:[/] {methodName} ([red]{lineCount} lines[/])");
        AnsiConsole.MarkupLine($"[green]Average method length:[/] {average:F2} lines");
        AnsiConsole.MarkupLine($"[green]Number of classes:[/] {amountOfClasses}");
        AnsiConsole.MarkupLine($"[green]TODO/FIXME comments found:[/] {amountOfPendingTasks}");
        AnsiConsole.MarkupLine($"[green]Properties count:[/] {properties}");
        AnsiConsole.MarkupLine($"[green]Fields count:[/] {fields}");
        AnsiConsole.MarkupLine($"[green]Comment density:[/] {density:F2}%");

        ConsoleUI.WaitForKey();
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

    /// <summary>
    /// Determine if a node contains the Constant modifier
    /// </summary>
    /// <param name="node">The very skeptical and sneaky node in question</param>
    /// <returns>True when it has the const modifier, else false</returns>
    private static bool IsConstDeclaration(SyntaxNode node)
    {
        return node switch
        {
            FieldDeclarationSyntax field => field.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)),
            LocalDeclarationStatementSyntax local => local.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)),
            _ => false
        };
    }

    /// <summary>
    /// Get the longest method and the average method length
    /// </summary>
    /// <param name="methods">Enumerator of methods (MethodDeclarationSyntax)</param>
    /// <returns>The method name and length of the longest method and the average method length</returns>
    private static (string methodName, int lineCount, double average) GetLongestMethodAndAverageMethodLength(IEnumerable<MethodDeclarationSyntax> methods)
    {
        Dictionary<string, int> linesCount = new();

        foreach (MethodDeclarationSyntax method in methods)
        {
            string methodName = method.Identifier.Text;
            int lineCount = method.SyntaxTree.GetLineSpan(method.Span).EndLinePosition.Line + 1;
            linesCount.Add(methodName, lineCount);
        }

        var highest = linesCount.MaxBy(kvp => kvp.Value);
        double average = linesCount.Values.ToArray().Average();

        return (highest.Key, highest.Value, average);
    }

    /// <summary>
    /// Get the amount of properties and fields
    /// </summary>
    /// <param name="root">Root node</param>
    /// <returns>The amount of properties and fields</returns>
    private static (int properties, int fields) GetAmountOfPropertiesAndFields(SyntaxNode root)
    {
        int properties = root.DescendantNodes().OfType<PropertyDeclarationSyntax>().Count();
        int fields = root.DescendantNodes().OfType<FieldDeclarationSyntax>().Count();

        return (properties, fields);
    }

    /// <summary>
    /// Calculate the comment density
    /// </summary>
    /// <param name="root"></param>
    /// <returns></returns>
    private static double CalculateCommentDensity(SyntaxNode root)
    {
        int commentLines = 0;
        int fullLines = 0;

        IEnumerable<SyntaxTrivia> comments = root.DescendantTrivia()
                        .Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia));

        // Determine how many lines of comments there are
        foreach (var comment in comments)
        {
            if (comment.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                commentLines++;
                continue;
            }

            var lineSpan = comment.SyntaxTree.GetLineSpan(comment.Span);
            var lineCount = lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line + 1;

            commentLines += lineCount;
        }

        var full = root.SyntaxTree.GetText();

        fullLines = full.Lines.Count > 0 ? full.Lines.Count : 1;

        return (double)commentLines / fullLines * 100;
    }
}