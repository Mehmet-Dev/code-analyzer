using Microsoft.CodeAnalysis;
using Spectre.Console;
using CodeAnalyzer.Helpers.ConsoleUI;
using CodeAnalyzer.Models;

namespace CodeAnalyzer;

public class AnalyzerCore
{
    private Analyzer _analyzer;
    private Dictionary<string, Action<ThresholdContext>> _flags;
    private Queue<Action<ThresholdContext>> _queue = new();
    private string[] _args;

    public AnalyzerCore(string[] args)
    {
        _args = args;
        Initialize();
    }

    private void Initialize()
    {
        bool foundRoot = false;
        bool runAll = true;
        List<string> chosenFlags = new();

        if (_args.Length == 0 || _args.Contains("--help"))
        {
            PrintOutInstructions();
            Environment.Exit(1);
        }

        if (_args.Contains("--bulk"))
        {
            BulkAnalyzer bulk = new(_args);
            Environment.Exit(1);
        }

        foreach (string arg in _args)
            {
                if (arg.StartsWith("--"))
                {
                    runAll = false;
                    chosenFlags.Add(arg);
                    continue;
                }
                else if (arg.EndsWith(".cs"))
                {
                    SyntaxNode? root = CheckRoot(arg);

                    if (root != null)
                    {
                        foundRoot = true;
                        _analyzer = new(root);
                    }
                }
            }

        if (!foundRoot)
        {
            ConsoleUI.PrintError("A valid .cs file wasn't provided. Please provide a path.");
            Environment.Exit(-1);
        }

        _flags = RegisterFlags();

        if (runAll)
        {
            foreach (var (key, action) in _flags)
            {
                _queue.Enqueue(action);
            }
        }
        else
        {
            foreach (string flag in chosenFlags)
            {
                if (_flags.TryGetValue(flag, out var action))
                {
                    _queue.Enqueue(action);
                }
                else
                {
                    ConsoleUI.PrintError($"Invalid flag: {flag}");
                    ConsoleUI.WaitForKey();
                }
            }
        }
    }

    public void Run()
    {
        Console.Clear();

        if (!File.Exists("config.cfg")) // If the file doesn't exist, quickly make the file 
            CreateConfig();

        ThresholdContext context = ParseConfig();
        

        foreach (var action in _queue)
        {
            action.Invoke(context);
            Console.Clear();
        }
    }

    /// <summary>
    /// Grab the root node of the file
    /// </summary>
    /// <param name="arg">Argument that ends with ".cs"</param>
    /// <returns>A syntaxnode which is the root</returns>
    private static SyntaxNode? CheckRoot(string arg)
    {
        if (File.Exists(arg))
        {
            try
            {
                return Analyzer.ReturnRoot(arg);
            }
            catch (InvalidDataException e)
            {
                ConsoleUI.PrintError($"Invalid file: {e.Message}");
                Environment.Exit(-1);
            }
            catch (UnauthorizedAccessException)
            {
                ConsoleUI.PrintError("Access denied to file.");
                Environment.Exit(-1);
            }
            catch (IOException)
            {
                ConsoleUI.PrintError("IO error reading the file.");
                Environment.Exit(-1);
            }
            catch (ArgumentException)
            {
                ConsoleUI.PrintError("Invalid argument when parsing file.");
                Environment.Exit(-1);
            }
            catch (Exception e)
            {
                ConsoleUI.PrintError($"Unexpected error: {e.Message}");
                Environment.Exit(-1);
            }
        }
        else
        {
            ConsoleUI.PrintError($"File {arg} not found");
            Environment.Exit(1);
        }

        return null;
    }

    private Dictionary<string, Action<ThresholdContext>> RegisterFlags()
    => new()
    {
        ["--methodLength"] = ctx => _analyzer.CheckMethodLengths(ctx.MethodLength),
        ["--params"] = _ => _analyzer.CheckParameterCount(),
        ["--magic"] = _ => _analyzer.CheckMagicNumbers(),
        ["--pending"] = _ => _analyzer.CheckPendingTasks(),
        ["--fileStats"] = _ => _analyzer.ShowFileStats(),
        ["--methodComplexity"] = ctx => _analyzer.CheckMethodComplexity(),
        ["--deadCode"] = _ => _analyzer.CheckDeadCode(),
        ["--duplicate"] = _ => _analyzer.CheckDuplicateStrings(),
        ["--methodDepth"] = ctx => _analyzer.CheckMethodDepth(ctx.MethodDepth),
        ["--genericNames"] = _ => _analyzer.CheckMethodNames(),
    };


    public static void CreateConfig()
    {
        string[] lines = ["MethodLength=15", "MethodDepth=3"];
        string path = "config.cfg";

        using StreamWriter writer = new(path);
        foreach (string line in lines)
            writer.WriteLine(line);
    }

    public static ThresholdContext ParseConfig()
    {
        var lines = File.ReadAllLines("config.cfg");
        var thresholds = new Dictionary<string, int>();

        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                continue;

            var parts = line.Split('=');
            if (parts.Length == 2 && int.TryParse(parts[1], out int value))
                thresholds[parts[0]] = value;
        }

        return new(thresholds["MethodLength"], thresholds["MethodDepth"]);
    }

    private void PrintOutInstructions()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold underline green]CodeAnalyzer Help[/]\n");

        AnsiConsole.MarkupLine("[bold yellow]Usage:[/]");
        AnsiConsole.MarkupLine("  [green]dotnet run -- [blue]<path-to-file.cs>[/] [purple]<flags>[/][/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold yellow]Example:[/]");
        AnsiConsole.MarkupLine("  [green]dotnet run -- Sample.cs --methodLength --params --magic[/]");
        AnsiConsole.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold cyan]Available Flags[/]")
            .AddColumn("[u]Flag[/]")
            .AddColumn("[u]Description[/]");

        table.AddRow("[purple]--methodLength[/]", "Checks method lengths against a threshold.");
        table.AddRow("[purple]--params[/]", "Checks methods for too many parameters.");
        table.AddRow("[purple]--magic[/]", "Detects magic numbers.");
        table.AddRow("[purple]--pending[/]", "Finds TODO, FIXME, HACK comments.");
        table.AddRow("[purple]--fileStats[/]", "Shows basic file stats (lines, comments, etc).");
        table.AddRow("[purple]--methodComplexity[/]", "Estimates method complexity (branching logic).");
        table.AddRow("[purple]--deadCode[/]", "Detects unreachable code.");
        table.AddRow("[purple]--duplicate[/]", "Warns about repeated string literals.");
        table.AddRow("[purple]--methodDepth[/]", "Flags deeply nested loops.");
        table.AddRow("[purple]--genericNames[/]", "Warns about vague method names like DoStuff.");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold yellow]Tips:[/]");
        AnsiConsole.MarkupLine("- You can run with no flags to run all checks.");
        AnsiConsole.MarkupLine("- Flags can be in any order.");
        AnsiConsole.MarkupLine("- Use [green]--help[/] to view this screen again.");
    }
}