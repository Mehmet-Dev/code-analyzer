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
        AnsiConsole.MarkupLine($"FIXME and TODO comments");

        if (comments.Count() == 0)
        {
            AnsiConsole.MarkupLine("No TODO/FIXME-style comments found.");
        }

        foreach (string comment in comments)
        {
            AnsiConsole.MarkupLine(comment);
        }

        ConsoleUI.WaitForKey();
    }

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