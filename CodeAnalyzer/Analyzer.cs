using CodeAnalyzer.Analyzers;
using CodeAnalyzer.Helpers.ConsoleUI;
using CodeAnalyzer.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Spectre.Console;

namespace CodeAnalyzer;

public class Analyzer
{
    private SyntaxNode _root;
    public JsonWriter writer;

    public Analyzer(SyntaxNode root, bool json = false)
    {
        _root = root;

        if (json)
        {
            writer = new(ReturnAllMethods(_root));
        }
    }

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

            writer?.UpdateLineLength(name, lineCount);
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

        var parameterCounts = ParameterCountAnalzer.Analyze(_root);

        foreach (var counts in parameterCounts)
        {
            var (name, count) = counts.Key;
            var (message, color) = counts.Value;

            AnsiConsole.MarkupLine($"[{color}]{name} has {count} parameters. {message}[/]");

            writer?.UpdateParameterCount(name, count);
        }

        ConsoleUI.WaitForKey();
    }

    /// <summary>
    /// Check for magic numbers.
    /// Magic numbers are number literals that aren't assigned to values.
    /// </summary>
    public void CheckMagicNumbers()
    {
        var full = MagicNumberAnalyzer.Analyze(_root);
        Dictionary<string, List<string>> raw = full.raw;
        Dictionary<string, string> magicNumbers = full.full;

        AnsiConsole.MarkupLine($"[bold yellow]Magic Number Detection[/]");

        foreach (var (name, message) in magicNumbers)
        {
            AnsiConsole.MarkupLine($"[italic blue]{name}[/]");
            AnsiConsole.MarkupLine($"{message}");

            if (writer != null)
            {
                var value = raw[name];
                if (value.Count > 0)
                {
                    writer.UpdateMagicNumbers(name, value);
                }
            }
            
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
        var full = PendingTasksAnalyzer.Analyze(_root);
        List<string> comments = full.full;
        List<string> raw = full.raw;

        AnsiConsole.MarkupLine($"[bold yellow]FIXME and TODO comments[/]");

        if (comments.Count == 0)
        {
            AnsiConsole.MarkupLine("No TODO/FIXME-style comments found.");
            ConsoleUI.WaitForKey();
            return;
        }

        foreach (string comment in comments)
        {
            AnsiConsole.MarkupLine(comment);
        }

        writer?.UpdatePendingTasks(raw);

        ConsoleUI.WaitForKey();
    }

    /// <summary>
    /// Check how complex your methods are.
    /// It checks for decision points (if, foreach, etc.)
    /// And uses an arbitrary value to determine how complex a method is.
    /// </summary>
    public void CheckMethodComplexity()
    {
        var full = ComplexityAnalyzer.Analyze(_root);
        List<string> lines = full.printing;
        Dictionary<string, ComplexityAnalysis> analysis = full.analysis;

        AnsiConsole.MarkupLine($"[bold yellow]Method complexity[/]");

        foreach (string line in lines)
        {
            AnsiConsole.MarkupLine(line);
        }

        if (writer != null)
        {
            foreach (var (name, a) in analysis)
            {
                writer.UpdateComplexity(name, a);
            }
        }

        ConsoleUI.WaitForKey();
    }

    /// <summary>
    /// Shows general file stats like total lines, amount of methods, etc.
    /// </summary>
    public void ShowFileStats()
    {
        var full = FileAnalyzer.Analyze(_root);
        List<string> writes = full.lines;
        FileStats stats = full.fileStats;

        foreach (string write in writes)
        {
            AnsiConsole.MarkupLine(write);
        }

        writer?.UpdateFileStats(stats);

        ConsoleUI.WaitForKey();
    }

    /// <summary>
    /// Display dead code information
    /// </summary>
    public void CheckDeadCode()
    {
        Dictionary<string, List<string>> methodWarnings = DeadCodeAnalyzer.Analyze(_root);
        AnsiConsole.MarkupLine($"[bold yellow]Dead code detection[/]");

        if (methodWarnings.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]No dead code detected![/]");
            ConsoleUI.WaitForKey();
            return;
        }

        foreach (var (method, warnings) in methodWarnings)
        {
            AnsiConsole.MarkupLine($"[blue]{method}[/]");

            foreach (string warning in warnings)
            {
                AnsiConsole.MarkupLine($"[red]- {warning}[/]");
            }

            writer?.UpdateDeadCode(method, warnings);
        }

        ConsoleUI.WaitForKey();
    }

    /// <summary>
    /// Check for duplicate string literals.
    /// Duplicate string literals are strings that aren't assigned to a variable,
    /// and that are repeated multiple times.
    /// It's better to assign them to values.
    /// </summary>
    public void CheckDuplicateStrings()
    {
        var strings = DuplicateStringAnalyzer.Analyze(_root);

        AnsiConsole.MarkupLine($"[bold yellow]Duplicate string literals[/]");

        if (strings.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]No duplicate string literals found![/]");
            ConsoleUI.WaitForKey();
            return;
        }

        foreach (var (text, amount) in strings)
        {
            AnsiConsole.MarkupLine($"[blue]\"{text}\"[/] is used [red]{amount}[/] times");
        }

        writer?.UpdateDuplicateStrings(strings);

        AnsiConsole.MarkupLine("\n[yellow]Consider using constant string variables if you're using them multiple times[/]");
        ConsoleUI.WaitForKey();
    }

    /// <summary>
    /// Check the nested depth of a file
    /// </summary>
    /// <param name="threshold">Threshold for results</param>
    public void CheckMethodDepth(int threshold)
    {
        var results = NestedLoopAnalyzer.Analyze(_root, 0);

        AnsiConsole.MarkupLine($"[bold yellow]Methods with deeply nested loops[/]");

        if (results.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]No methods found with deeply nested loops![/]");
            return;
        }

        foreach (var (methodName, amount) in results)
        {
            if (amount < 3)
                AnsiConsole.MarkupLine($"[blue]{methodName}[/] has a depth of [green]{amount}[/]");
            else
                AnsiConsole.MarkupLine($"[blue]{methodName}[/] has a depth of [red]{amount}[/], consider reconstructing");

            writer?.UpdateDepth(methodName, amount);
        }

        ConsoleUI.WaitForKey();
    }

    /// <summary>
    /// Check method names for generic names.
    /// Generic names are VERY BAD!!!!!!!!!!!!!!!
    /// </summary>
    public void CheckMethodNames()
    {
        List<string> methods = MethodNameAnalyzer.Analyze(_root);

        AnsiConsole.MarkupLine("[bold yellow]Method name analyzer[/]");

        if (methods.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]No generic method names found![/]");
            ConsoleUI.WaitForKey();
            return;
        }

        foreach (string method in methods)
        {
            AnsiConsole.MarkupLine($"[blue]Method name [red]\"{method}\"[/] is too generic[/]");

            writer?.UpdateGenericName(method);
        }

        AnsiConsole.MarkupLine($"\n[yellow]It's not good practice to use method names that don't explain what they're supposed to do[/]");
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
    /// Return a list of MethodAnalysisResult used for json output
    /// </summary>
    /// <param name="root">Node root to check</param>
    /// <returns>A list of MethodAnalysisResults</returns>
    public static List<MethodAnalysisResult> ReturnAllMethods(SyntaxNode root)
    {
        List<MethodAnalysisResult> results = new();
        IEnumerable<MethodDeclarationSyntax> methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            MethodAnalysisResult result = new(method.Identifier.Text);
            results.Add(result);
        }

        return results;
    }
}