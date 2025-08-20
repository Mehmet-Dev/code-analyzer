namespace CodeAnalyzer.Models;

/// <summary>
/// Another class used for json output, saves the results for every method into a nicely structured class
/// </summary>
public class MethodAnalysisResult
{
    public string MethodName { get; set; }
    public ComplexityAnalysis Complexity { get; set; } // see ComplexityAnalyzer
    public int LineLength { get; set; } // see MethodLengthAnalyzer
    public int ParameterCount { get; set; } // see ParamterCountAnalyzer
    public List<string> MagicNumbers { get; set; } = new(); // see MagicNumberAnalyzer
    public List<string> DeadCode { get; set; } = new(); // see DeadCodeAnalyzer
    public int Depth { get; set; } // see NestedLoopAnalyzer
    public bool IsGenericName { get; set; } // see MethodNameAnalyzer

    public MethodAnalysisResult(string methodName) { MethodName = methodName; }

    /// <summary>
    /// Used to determine which methods have valid data
    /// </summary>
    /// <seealso cref="JsonWriter.SaveToFile"/=>
    /// <returns>True if there is valid data, false if everything is empty</returns>
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