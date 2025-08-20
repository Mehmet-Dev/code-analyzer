namespace CodeAnalyzer.Models;

/// <summary>
/// Simple class used for CFG file threshold checking
/// <seealso cref="AnalyzerCore.CreateConfig"/>
/// <seealso cref="AnalyzerCore.ParseConfig"/>
/// </summary>
public class ThresholdContext
{
    public int MethodLength = 15; // If all else fails, assign base values 
    public int MethodDepth = 3; // idk

    public ThresholdContext(int length, int depth)
    {
        MethodLength = length;
        MethodDepth = depth;
    }

    public ThresholdContext()
    { }
}