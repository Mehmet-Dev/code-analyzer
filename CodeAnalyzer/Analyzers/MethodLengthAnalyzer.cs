using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalyzer.Analyzers;

public static class MethodLengthAnalyzer
{
    public static Dictionary<string, int> Analyze(SyntaxNode root)
    {
        Dictionary<string, int> methodLengths = new();
        IEnumerable<MethodDeclarationSyntax> methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var lineSpan = method.SyntaxTree.GetLineSpan(method.Span);
            var lineCount = lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line + 1;

            methodLengths.Add(method.Identifier.Text, lineCount);
        }

        return methodLengths;
    }
}