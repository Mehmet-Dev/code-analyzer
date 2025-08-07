namespace CodeAnalyzer.Models;

public class ThresholdContext
{
    public int MethodLength = 15;
    public int MethodDepth = 3;

    public ThresholdContext(int length, int depth)
    {
        MethodLength = length;
        MethodDepth = depth;
    }

    public ThresholdContext()
    {}
}