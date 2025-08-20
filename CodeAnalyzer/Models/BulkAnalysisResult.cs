namespace CodeAnalyzer.Models;

/// <summary>
/// Class used for bulk JSON analysis
/// </summary>
public class BulkAnalysisResult
{
    public Dictionary<string, int> MethodLengths { get; set; } = new();
    public Dictionary<string, int> ParameterCounts { get; set; } = new();
    public Dictionary<string, List<string>> MagicNumbers { get; set; } = new();
    public List<string> PendingTasks { get; set; } = new();
    public List<FileStats> FileStats { get; set; } = new();
    public Dictionary<string, ComplexityAnalysis> MethodComplexities { get; set; } = new();
    public Dictionary<string, List<string>> DeadCode { get; set; } = new();
    public Dictionary<string, List<string>> GenericMethodNames { get; set; } = new();
}
