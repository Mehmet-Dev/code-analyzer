using System.Text.Json;
using System.Text.Json.Serialization;
using CodeAnalyzer.Helpers.ConsoleUI;
using Spectre.Console;

namespace CodeAnalyzer.Models;

/// <summary>
/// Class used for single file JSON analysis
/// </summary>
public class JsonWriter
{
    private Dictionary<string, MethodAnalysisResult> _methodDict = new(); // The analysis results of methods
    private List<string> _pendingTasks = new(); // All pending tasks
    private FileStats _fileStats = new(); // File results
    private Dictionary<string, int> _duplicateStrings; // Dictionary of duplicate string literals

    /// <summary>
    /// It takes a bare list of MethodAnalysisResults
    /// <see cref="Analyzer.ReturnAllMethods"/>
    /// </summary>
    public JsonWriter(List<MethodAnalysisResult> results)
    {
        _methodDict = results.ToDictionary(m => m.MethodName);
    }

    public void UpdatePendingTasks(List<string> tasks)
        => _pendingTasks = tasks;

    public void UpdateFileStats(FileStats stats)
        => _fileStats = stats;

    public void UpdateDuplicateStrings(Dictionary<string, int> results)
        => _duplicateStrings = results;

    public void UpdateComplexity(string methodName, ComplexityAnalysis complexity)
    {
        if (_methodDict.TryGetValue(methodName, out var method))
            method.Complexity = complexity;
    }

    public void UpdateMagicNumbers(string methodName, List<string> results)
    {
        if (_methodDict.TryGetValue(methodName, out var method))
            method.MagicNumbers = results;
    }

    public void UpdateLineLength(string methodName, int lineLength)
    {
        if (_methodDict.TryGetValue(methodName, out var method))
            method.LineLength = lineLength;
    }

    public void UpdateParameterCount(string methodName, int paramCount)
    {
        if (_methodDict.TryGetValue(methodName, out var method))
            method.ParameterCount = paramCount;
    }

    public void UpdateDeadCode(string methodName, List<string> deadCode)
    {
        if (_methodDict.TryGetValue(methodName, out var method))
            method.DeadCode = deadCode;
    }

    public void UpdateDepth(string methodName, int depth)
    {
        if (_methodDict.TryGetValue(methodName, out var method))
            method.Depth = depth;
    }

    public void UpdateGenericName(string methodName)
    {
        if (_methodDict.TryGetValue(methodName, out var method))
            method.IsGenericName = true;
    }

    /// <summary>
    /// Save the results to a json file
    /// </summary>
    public void SaveToFile()
    {
        var exportObject = new Dictionary<string, object>();

        var analyzedMethods = _methodDict.Values
        .Where(m => m.HasAnalysisData())
        .ToList(); // Check which methods have actual analysis data, if not, ignore them

        if (analyzedMethods.Count > 0)
            exportObject["Methods"] = analyzedMethods;

        if (_pendingTasks.Count > 0)
            exportObject["PendingTasks"] = _pendingTasks;

        if (_fileStats != null)
            exportObject["FileStats"] = _fileStats;

        if (_duplicateStrings != null && _duplicateStrings.Count > 0)
            exportObject["DuplicateStrings"] = _duplicateStrings;

        var options = new JsonSerializerOptions
        {
            WriteIndented = true, // So it's still readable
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        string json = JsonSerializer.Serialize(exportObject, options);
        string fileName = $"analysis_results_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.json"; // i.e. analysis_results_2025-12-22_20:55:53.json

        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads", // Save the file into the downloads folder
            fileName
        );

        File.WriteAllText(path, json);
        AnsiConsole.WriteLine($"JSON saved to: {path}");
        Environment.Exit(0); // Finish execution
    }
}