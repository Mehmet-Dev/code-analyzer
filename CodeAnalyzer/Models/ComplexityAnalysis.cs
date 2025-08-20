namespace CodeAnalyzer.Models;

/// <summary>
/// A class used for JSON output again, it's jsut a nice way to collect everything
/// </summary>
public class ComplexityAnalysis
{
    public int TotalComplexity { get; set; }
    public int IfStatements { get; set; }
    public int ForStatements { get; set; }
    public int WhileAndDoStatements { get; set; }
    public int SwitchStatements { get; set; }
    public int LogicOperators { get; set; }

    public ComplexityAnalysis(int total, int ifs, int fors, int whiles, int switches, int logics)
    {
        TotalComplexity = total;
        IfStatements = ifs;
        ForStatements = fors;
        WhileAndDoStatements = whiles;
        SwitchStatements = switches;
        LogicOperators = logics;
    }

    public ComplexityAnalysis() { }
}