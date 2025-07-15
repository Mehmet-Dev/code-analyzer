using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalyzer.Analyzers;

public static class MagicNumberAnalyzer
{
    public static Dictionary<string, string> CheckMagicNumbers(SyntaxNode root)
    {
        Dictionary<string, string> magicNumbers = new();
        HashSet<SyntaxKind> kindsToIgnore = new()
        {
            SyntaxKind.EnumMemberDeclaration,
            SyntaxKind.EnumDeclaration,
            SyntaxKind.AttributeArgument,
            SyntaxKind.Parameter,
            SyntaxKind.CaseSwitchLabel,
            SyntaxKind.UsingDirective,
            SyntaxKind.VariableDeclarator,
            SyntaxKind.CompilationUnit, // <- this is unnecessary but just a sanity check
            SyntaxKind.SwitchSection,
        };

        IEnumerable<MethodDeclarationSyntax> methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        List<int> filterOut = [0, 1];

        foreach (var method in methods)
        {
            bool magicNumberFound = false;
            IEnumerable<LiteralExpressionSyntax> numerics = method.DescendantNodes()
                            .OfType<LiteralExpressionSyntax>()
                            .Where(lit => lit.IsKind(SyntaxKind.NumericLiteralExpression)); // Filter out everything that isn't a literal number

            string message = "";
            foreach (LiteralExpressionSyntax numeric in numerics)
            {
                var value = numeric.Token.Value;
                if (value is int intValue)
                {
                    if (filterOut.Contains(intValue)) // if the token is that of 0 or 1 (usually true/false) skip
                        continue;
                }

                IEnumerable<SyntaxNode> ancestors = numeric.Ancestors().Take(5); // Limit to 5 at most as to not traverse way too high into the compilation unit
                IEnumerable<SyntaxKind> kinds = ancestors.Select(a => a.Kind());

                if (kinds.Any(a => kindsToIgnore.Contains(a))) // If the kind is that of the ones to ignore, skip the loop
                    continue;

                var ancestor = ancestors.FirstOrDefault(a =>
                a.IsKind(SyntaxKind.FieldDeclaration) ||
                a.IsKind(SyntaxKind.LocalDeclarationStatement)); // Grab a kind that's either a field or local declaration

                if (ancestor != null && IsConstDeclaration(ancestor)) // If it has a const modifier, skip this
                {
                    continue;
                }

                // After all the checks give an error
                var lineSpan = numeric.SyntaxTree.GetLineSpan(numeric.Span);
                int lineNumber = lineSpan.StartLinePosition.Line + 1;

                message += $"[red]- Magic number {numeric.Token.Value} found on line {lineNumber}[/]";
                magicNumberFound = true;
            }

            if (!magicNumberFound)
                message += "[green]- No magic number found[/]";

            magicNumbers.Add(method.Identifier.Text, message);
        }

        return magicNumbers;
    }

    /// <summary>
    /// Determine if a node contains the Constant modifier
    /// </summary>
    /// <param name="node">The very skeptical and sneaky node in question</param>
    /// <returns>True when it has the const modifier, else false</returns>
    private static bool IsConstDeclaration(SyntaxNode node)
    {
        return node switch
        {
            FieldDeclarationSyntax field => field.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)),
            LocalDeclarationStatementSyntax local => local.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)),
            _ => false
        };
    }
}