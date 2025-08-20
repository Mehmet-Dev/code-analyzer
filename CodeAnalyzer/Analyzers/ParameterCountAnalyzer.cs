using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalyzer.Analyzers;

public static class ParameterCountAnalzer
{
    /// <summary>
    /// Analyze the amount of parameters a method has
    /// </summary>
    /// <param name="root">Node root to analyze</param>
    /// <returns>A dictionary where the key is method name and parameter count, and the value is the message and color to print</returns>
    public static Dictionary<(string name, int count), (string message, string color)> Analyze(SyntaxNode root)
    {
        Dictionary<(string name, int count), (string message, string color)> list = new();
        IEnumerable<MethodDeclarationSyntax> methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            IEnumerable<ParameterSyntax> parameters = method.ParameterList.Parameters;

            int count = parameters.Count(); // Amount of parameters

            var result = ParameterCountString(count);

            list.Add((method.Identifier.Text, count), result);
        }

        return list;
    }

    /// <summary>
    /// Make a nicely formatted message
    /// </summary>
    /// <param name="count">Amount of parameters of a method</param>
    /// <returns>A tuple where message is the message to print and the color is the color it needs to be outputted as using AnsiConsole</returns>
    private static (string message, string color) ParameterCountString(int count)
    {
        if (count >= 7)
            return ("Excessive parameters; strongly reconsider redesigning the method.", "red");
        else if (count >= 5)
            return ("High parameter count; refactoring is recommended.", "bold yellow");
        else if (count >= 3)
            return ("Moderate parameter count; consider simplifying if possible unless necessary.", "yellow");
        else
            return ("", "green");
    }
}