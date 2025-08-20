using System.Text.Json;
using System.Text.Json.Serialization;
using CodeAnalyzer.Analyzers;
using CodeAnalyzer.Helpers.ConsoleUI;
using CodeAnalyzer.Models;
using Microsoft.CodeAnalysis;

namespace CodeAnalyzer;

/// <summary>
/// Class used for bulk analysis and JSON output
/// </summary>
public class BulkJsonAnalyzer
{
    private List<string> _flags = new();
    private List<string> _files;
    private BulkAnalysisResult _result = new();

    public BulkJsonAnalyzer(string[] args)
    {
        Initialize(args);
        Run();
    }

    /// <summary>
    /// Getting so tired to explain everything
    /// </summary>
    /// <param name="args">Array of arguments</param>
    private void Initialize(string[] args)
    {
        List<string> arguments = args.ToList();
        arguments.Remove("--bulk");
        arguments.Remove("--json");

        List<string> paths = new();
        foreach (var arg in arguments)
        {
            if (arg.StartsWith("--"))
                _flags.Add(arg);
            else
                paths.Add(arg);
        }

        if (paths.Count > 1) // If multiple paths were provided, quit execution
        {
            ConsoleUI.PrintError("Error: multiple paths or broken flags have been provided.\nTry again.");
            Environment.Exit(-1);
        }

        if (paths.Count == 0) // If no path was provided blow up their house
        {
            ConsoleUI.PrintError("Error: no path was provided.\nTry again.");
            Environment.Exit(-1);
        }

        if (_flags.Count == 0)
            _flags = AllFlags();

        if (!Directory.Exists(paths[0])) // If the directory doesn't exist... yada yada
        {
            ConsoleUI.PrintError($"Folder '{paths[0]} not found.");
            Environment.Exit(-1);
        }

        _files = Directory.GetFiles(paths[0], "*.cs", SearchOption.AllDirectories).ToList();
    }

    private void Run()
    {
        if (!File.Exists("config.cfg"))
            AnalyzerCore.CreateConfig();

        var thresholds = AnalyzerCore.ParseConfig();

        var actions = new Dictionary<string, Action>
        {
            ["--methodLength"] = () => CheckMethodLengths(thresholds.MethodLength),
            ["--params"] = CheckParameterCount,
            ["--magic"] = CheckMagicNumbers,
            ["--pending"] = () => { CheckPendingTasks(); },
            ["--fileStats"] = () => { CheckFileStats(); },
            ["--methodComplexity"] = () => { CheckMethodComplexity(); },
            ["--deadCode"] = () => { CheckDeadCode(); },
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
                continue;
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        string json = JsonSerializer.Serialize(_result, options);
        string fileName = $"bulk_analysis_results_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.json";

        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads",
            fileName
        );

        File.WriteAllText(path, json);
        Console.WriteLine($"JSON saved to: {path}");
        ConsoleUI.WaitForKey();
        Environment.Exit(0);
    }

    /*
        Same with BulkAnalyzer, these are just methods used to nicely set everything into their respected spots
    */

    private void CheckMethodLengths(int threshold)
    {
        var copyCounters = new Dictionary<string, int>();

        foreach (string path in _files)
        {
            SyntaxNode root = Analyzer.ReturnRoot(path);
            var methodLengths = MethodLengthAnalyzer.Analyze(root);

            foreach (var kvp in methodLengths)
            {
                string key = kvp.Key;

                if (_result.MethodLengths.ContainsKey(key))
                {
                    if (!copyCounters.ContainsKey(key))
                        copyCounters[key] = 1;

                    copyCounters[key]++;
                    key += $"({copyCounters[key]})";
                }

                _result.MethodLengths[key] = kvp.Value;
            }
        }
    }

    private void CheckParameterCount()
    {
        var copyCounters = new Dictionary<string, int>();

        foreach (string path in _files)
        {
            SyntaxNode root = Analyzer.ReturnRoot(path);
            var paramResults = ParameterCountAnalzer.Analyze(root);

            foreach (var kvp in paramResults)
            {
                string methodName = kvp.Key.name;

                // Handle duplicate method names
                if (_result.ParameterCounts.ContainsKey(methodName))
                {
                    if (!copyCounters.ContainsKey(methodName))
                        copyCounters[methodName] = 1;

                    copyCounters[methodName]++;
                    methodName += $"({copyCounters[methodName]})";
                }

                _result.ParameterCounts[methodName] = kvp.Key.count;
            }
        }
    }

    private void CheckMagicNumbers()
    {
        foreach (var path in _files)
        {
            SyntaxNode root = Analyzer.ReturnRoot(path);
            var (rawResults, _) = MagicNumberAnalyzer.Analyze(root); // Use the raw dictionary

            foreach (var kvp in rawResults)
            {
                // Combine method name with file name for uniqueness
                string key = $"{kvp.Key} - {Path.GetFileName(path)}";

                if (_result.MagicNumbers.ContainsKey(key))
                {
                    _result.MagicNumbers[key].AddRange(kvp.Value);
                }
                else
                {
                    _result.MagicNumbers[key] = new List<string>(kvp.Value);
                }
            }
        }
    }

    private void CheckFileStats()
    {
        foreach (var path in _files)
        {
            SyntaxNode root = Analyzer.ReturnRoot(path);
            var (fileStats, _) = FileAnalyzer.Analyze(root);
            fileStats.FileName = Path.GetFileName(path);

            _result.FileStats.Add(fileStats);
        }
    }


    private void CheckPendingTasks()
    {
        foreach (var path in _files)
        {
            var fileName = Path.GetFileName(path);
            var (rawTasks, _) = PendingTasksAnalyzer.Analyze(Analyzer.ReturnRoot(path));

            // Prepend the filename to each raw task
            foreach (var task in rawTasks)
            {
                _result.PendingTasks.Add($"{fileName}: {task}");
            }
        }
    }



    private void CheckMethodComplexity()
    {
        // Initialize the dictionary in your BulkAnalysisResult
        _result.MethodComplexities = new Dictionary<string, ComplexityAnalysis>();

        foreach (var path in _files)
        {
            SyntaxNode root = Analyzer.ReturnRoot(path);
            string fileName = Path.GetFileName(path);

            var (analysis, _) = ComplexityAnalyzer.Analyze(root);

            // Store under file name to avoid collisions
            foreach (var kvp in analysis)
            {
                string key = $"{fileName} - {kvp.Key}";
                _result.MethodComplexities[key] = kvp.Value;
            }
        }
    }

    private void CheckDeadCode()
    {
        foreach (string path in _files)
        {
            SyntaxNode root = Analyzer.ReturnRoot(path);
            var methodWarnings = DeadCodeAnalyzer.Analyze(root);
            string fileName = Path.GetFileName(path);

            foreach (var kvp in methodWarnings)
            {
                string key = $"{fileName} - {kvp.Key}"; // File + method to avoid collisions
                _result.DeadCode[key] = kvp.Value;
            }
        }
    }

    private void CheckMethodNames()
    {
        foreach (var path in _files)
        {
            string fileName = Path.GetFileName(path);
            var genericMethods = MethodNameAnalyzer.Analyze(Analyzer.ReturnRoot(path));

            if (genericMethods.Count > 0)
            {
                _result.GenericMethodNames[fileName] = genericMethods;
            }
        }
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