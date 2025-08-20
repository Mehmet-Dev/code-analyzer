namespace CodeAnalyzer.Models;

public class MethodAnalysisResult
{
    public string MethodName { get; set; }
    public ComplexityAnalysis Complexity { get; set; }
    public int LineLength { get; set; }
    public int ParameterCount { get; set; }
    public List<string> MagicNumbers { get; set; } = new();
    public List<string> DeadCode { get; set; } = new();
    public int Depth { get; set; }
    public bool IsGenericName { get; set; }

    public MethodAnalysisResult(string methodName) { MethodName = methodName; }

    public bool HasAnalysisData()
    {
        return LineLength > 0
            || ParameterCount > 0
            || (MagicNumbers?.Any() ?? false)
            || (DeadCode?.Any() ?? false)
            || Depth > 0
            || IsGenericName
            || Complexity != null;
    }

}