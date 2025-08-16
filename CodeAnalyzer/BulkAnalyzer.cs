using CodeAnalyzer.Analyzers;
using CodeAnalyzer.Helpers.ConsoleUI;
using Microsoft.CodeAnalysis;
using Spectre.Console;

namespace CodeAnalyzer;

public class BulkAnalyzer
{
    private List<string> _flags = new();
    private List<string> _files;
    public BulkAnalyzer(string[] args)
    {
        Initialize(args);
        Run();
    }

    private void Initialize(string[] args)
    {
        List<string> arguments = args.ToList<string>();

        arguments.Remove("--bulk");

        List<string> paths = new();
        foreach (var arg in arguments)
        {
            if (arg.StartsWith("--"))
                _flags.Add(arg);
            else
                paths.Add(arg);
        }

        if (paths.Count > 1)
        {
            ConsoleUI.PrintError("Error: multiple paths or broken flags have been provided.\nTry again.");
            Environment.Exit(-1);
        }

        if (paths.Count == 0)
        {
            ConsoleUI.PrintError("Error: no path was provided.\nTry again.");
            Environment.Exit(-1);
        }

        if (_flags.Count == 0)
                _flags = AllFlags();

        if (!Directory.Exists(paths[0]))
        {
            ConsoleUI.PrintError($"Folder '{paths[0]}' not found.");
            Environment.Exit(-1);
        }

        _files = Directory.GetFiles(paths[0], "*.cs", SearchOption.AllDirectories).ToList();
    }

    private void Run()
    {
        if (!File.Exists("config.cfg")) // If the file doesn't exist, quickly make the file 
            AnalyzerCore.CreateConfig();

        var thresholds = AnalyzerCore.ParseConfig();

        var actions = new Dictionary<string, Action>
        {
            ["--methodLength"] = () => CheckMethodLengths(thresholds.MethodLength),
            ["--params"] = CheckParameterCount,
            ["--magic"] = CheckMagicNumbers,
            ["--pending"] = () => { CheckPendingTasks(); },
            ["--fileStats"] = () => { Error(); },
            ["--methodComplexity"] = () => { CheckMethodComplexity(); },
            ["--deadCode"] = () => { Error(); },
            ["--duplicate"] = () => { Error(); },
            ["--methodDepth"] = () => { Error(); },
            ["--genericNames"] = () => { CheckMethodNames(); },
        };

        foreach (string flag in _flags)
        {
            Console.Clear();
            if (actions.TryGetValue(flag, out var action))
            {
                action();
            }
            else
            {
                ConsoleUI.PrintError($"Unknown flag: {flag}");
                ConsoleUI.WaitForKey();
            }
        }
    }

    private void CheckMethodLengths(int threshold)
    {
        Dictionary<string, int> results = new();
        int copy = 1;

        foreach (string path in _files)
        {
            SyntaxNode root = Analyzer.ReturnRoot(path);
            var result = MethodLengthAnalyzer.Analyze(root);

            foreach (var kvp in result)
            {
                if (results.ContainsKey(kvp.Key))
                {
                    results[kvp.Key + $"({copy})"] = kvp.Value;
                    copy++;
                }
                else
                    results[kvp.Key] = kvp.Value;
            }
        }

        var sortedResults = results.OrderByDescending(r => r.Value).ToDictionary();
        var table = new Table();

        table.AddColumn("Method");
        table.AddColumn("Lines");

        foreach (var (name, lineCount) in sortedResults)
        {
            if (lineCount > threshold)
                table.AddRow($"[red]{name}[/]", $"[bold red]{lineCount}[/]");
            else
                table.AddRow($"[green]{name}[/]", $"[green]{lineCount}[/]");
        }

        AnsiConsole.MarkupLine($"[bold yellow]Method Length Report[/]");
        AnsiConsole.Write(table);
        ConsoleUI.WaitForKey();
    }

    private void CheckParameterCount()
    {
        List<(string name, int count, string color)> results = new();
        List<string> methods = new();

        int copy = 1;
        foreach (string path in _files)
        {
            SyntaxNode root = Analyzer.ReturnRoot(path);
            var result = ParameterCountAnalzer.Analyze(root);

            foreach (var kvp in result)
            {
                if (methods.Contains(kvp.Key.name))
                {
                    results.Add((kvp.Key.name + $" ({copy})", kvp.Key.count, kvp.Value.color));
                    copy++;
                }
                else
                    results.Add((kvp.Key.name, kvp.Key.count, kvp.Value.color));
            }
        }

        var sortedResults = results.OrderByDescending(r => r.count).ToList();

        var table = new Table();
        table.AddColumn("Method");
        table.AddColumn("Parameter count");

        foreach (var (name, count, color) in sortedResults)
            table.AddRow($"[{color}]{name}[/]", $"[{color}]{count}[/]");

        AnsiConsole.MarkupLine($"[bold yellow]Parameter Count Report[/]");
        AnsiConsole.Write(table);
        ConsoleUI.WaitForKey();
    }

    private void CheckMagicNumbers()
    {
        Dictionary<string, string> results = new();

        foreach (var path in _files)
        {
            SyntaxNode root = Analyzer.ReturnRoot(path);
            var result = MagicNumberAnalyzer.Analyze(root);

            foreach (var kvp in result)
            {
                string key = $"{kvp.Key} - {Path.GetFileName(path)}";
                if (results.ContainsKey(key))
                    results[key] += "\n" + kvp.Value;
                else
                    results[key] = kvp.Value;
            }
        }

        AnsiConsole.MarkupLine($"[bold yellow]Magic number detection[/]");

        foreach (var (method, number) in results)
        {
            AnsiConsole.MarkupLine($"[italic blue]{method}[/]");
            AnsiConsole.MarkupLine($"{number}");
        }
        ConsoleUI.WaitForKey();
    }

    private void CheckPendingTasks()
    {
        List<string> results = new();

        foreach (var path in _files)
        {
            var result = PendingTasksAnalyzer.Analyze(Analyzer.ReturnRoot(path));

            foreach (var item in result)
                results.Add(item);
        }

        AnsiConsole.MarkupLine($"[bold yellow]Pending tasks[/]");

        foreach (string result in results)
            AnsiConsole.MarkupLine(result);

        ConsoleUI.WaitForKey();
    }

    private static void Error()
    {
        ConsoleUI.PrintError("This analysis cannot be used in bulk.");
        ConsoleUI.WaitForKey();
    }

    private void CheckMethodComplexity()
    {
        List<(int complexity, string text)> results = new();

        foreach (var path in _files)
        {
            string fileName = Path.GetFileName(path);
            var result = ComplexityAnalyzer.CheckFileComplexity(Analyzer.ReturnRoot(path));

            results.Add((result, DetermineFileComplexity(result, fileName)));
        }

        foreach (var (count, toPrint) in results.OrderBy(r => r.complexity))
            AnsiConsole.MarkupLine(toPrint);

        ConsoleUI.WaitForKey();
    }

    private void CheckMethodNames()
    {
        Dictionary<string, List<string>> results = new();

        foreach (var path in _files)
        {
            string fileName = Path.GetFileName(path);
            var result = MethodNameAnalyzer.Analyze(Analyzer.ReturnRoot(path));

            if (result.Count > 0)
                results.Add(fileName, result);
        }

        AnsiConsole.MarkupLine("\n[bold underline yellow]Generic Method Name Analysis[/]\n");

        foreach (var (file, result) in results)
        {
            // File name in bold cyan
            AnsiConsole.MarkupLine($"[bold cyan]{file}[/]");

            foreach (string method in result)
            {
                // Method name in magenta, 'found!' in green
                AnsiConsole.MarkupLine($"  [magenta]{method}[/] [green]found![/]");
            }

            // Add a subtle separator between files
            AnsiConsole.MarkupLine("[grey]----------------------------------------[/]");
        }

        ConsoleUI.WaitForKey();
    }


    private static string DetermineFileComplexity(int total, string fileName)
    {
        if (total > 100)
            return $"[bold red]{fileName} - File Complexity: {total} (Critical)[/]";
        else if (total >= 51)
            return $"[red]{fileName} - File Complexity: {total} (Complex)[/]";
        else if (total >= 21)
            return $"[yellow]{fileName} - File Complexity: {total} (Moderate)[/]";
        else
            return $"[green]{fileName} - File Complexity: {total} (Easy)[/]";
    }

    private static List<string> AllFlags() => new()
    {
        "--methodLength",
        "--params",
        "--magic",
        "--pending",
        "--fileStats",
        "--methodComplexity",
        "--deadCode",
        "--duplicate",
        "--methodDepth",
        "--genericNames"
    };
}