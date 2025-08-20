using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalyzer.Analyzers;

public static class MethodNameAnalyzer
{
    /// <summary>
    /// Analyzes the method names for generic naming
    /// </summary>
    /// <param name="root">Root node to analyze</param>
    /// <returns>List of strings where a string is a method name</returns>
    public static List<string> Analyze(SyntaxNode root)
    {
        List<string> results = new();
        var genericMethodNames = new List<string> // Names that are way too generic
        {
            "DoStuff",
            "HandleIt",
            "ProcessData",
            "Execute",
            "Run",
            "PerformAction",
            "Manage",
            "Operate",
            "Work",
            "Handle",
            "Process",
            "Action",
            "ExecuteTask",
            "Perform",
            "Start",
            "Stop",
            "Init",
            "Setup",
            "Update",
            "Calculate"
        };

        IEnumerable<MethodDeclarationSyntax> methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var methodName = method.Identifier.Text;

            if (genericMethodNames.Contains(methodName))
            {
                results.Add(methodName);
            }
        }
        return results;
    }
}