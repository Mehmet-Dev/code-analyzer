using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalyzer.Analyzers;

public static class ParameterCountAnalzer
{
    public static Dictionary<(string name, int count), (string message, string color)> Analyze(SyntaxNode root)
    {
        Dictionary<(string name, int count), (string message, string color)> list = new();
        IEnumerable<MethodDeclarationSyntax> methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            IEnumerable<ParameterSyntax> parameters = method.ParameterList.Parameters;

            int count = parameters.Count();

            var result = ParameterCountString(count);

            list.Add((method.Identifier.Text, count), result);
        }

        return list;
    }

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