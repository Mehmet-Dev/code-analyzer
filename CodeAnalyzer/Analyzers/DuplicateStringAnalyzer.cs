using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalyzer.Analyzers;

public static class DuplicateStringAnalyzer
{
    /// <summary>
    /// Analyzes the file for duplicate string literals
    /// String literals are strings that aren't assigned to constant values
    /// </summary>
    /// <param name="root">Root node of file</param>
    /// <returns>A dictionary where the key is the string literal, and int is the amount of times (when it's used 3+ times)</returns>
    public static Dictionary<string, int> Analyze(SyntaxNode root)
    {
        Dictionary<string, int> literalCounts = new();
        IEnumerable<LiteralExpressionSyntax> strings = root.DescendantNodes().OfType<LiteralExpressionSyntax>().Where(lit => lit.IsKind(SyntaxKind.StringLiteralExpression)); // Get all strings

        foreach (LiteralExpressionSyntax literal in strings) // Iterate over each string instance
        {
            string text = (string)literal.Token.Value!;
            if (text == "" || string.IsNullOrWhiteSpace(text)) // Skip whitespace or empty ones
                continue;

            if (literalCounts.ContainsKey(text)) // If it's already inserted into the dictionary, add to the value
                literalCounts[text]++;
            else // Else, make a new one
                literalCounts.Add(text, 1);
        }

        literalCounts = literalCounts
            .Where(kvp => kvp.Value > 2)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value); // Only get the ones that have more than 2 instances

        return literalCounts;
    }
}