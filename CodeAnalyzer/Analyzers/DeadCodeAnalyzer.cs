using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalyzer.Analyzers;

public static class DeadCodeAnalyzer
{
    /// <summary>
    /// Checks for dead code in method bodies
    /// </summary>
    /// <param name="root">Root syntax node</param>
    /// <returns>A dictionary where the key is the method name and the value is a list of warnings</returns>
    public static Dictionary<string, List<string>> Analyze(SyntaxNode root)
    {
        Dictionary<string, List<string>> methodWarnings = new();
        IEnumerable<MethodDeclarationSyntax> methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (var method in methods) // Loop through every method
        {
            BlockSyntax? body = method.Body;
            if (body == null) continue; // Check whether the method has a body

            List<string> warnings = new();

            AnalyzeBlock(body, warnings); // Check every statement of the method

            if (warnings.Any())
            {
                methodWarnings.Add(method.Identifier.Text, warnings);
            }
        }

        return methodWarnings;
    }

    private static bool AnalyzeBlock(BlockSyntax body, List<string> warnings)
    {
        bool terminated = false; // If terminated is set to true BEFORE the last statement, it usually indicates that there's dead code
                                 // Depends on the type of body

        foreach (var statement in body.Statements)
        {
            if (terminated)
            {
                var line = statement?.GetLocation()?.GetLineSpan().StartLinePosition.Line + 1 ?? -1;
                warnings.Add($"[red]Unreachable code detected on line {line}[/]");
                continue;
            }

            terminated = AnalyzeStatement(statement, warnings);
        }

        return terminated;
    }

    private static bool AnalyzeStatement(StatementSyntax statement, List<string> warnings)
    {
        switch (statement)
        {
            case ReturnStatementSyntax:
            case ThrowStatementSyntax:
                return true;

            case BreakStatementSyntax:
                return !IsInsideValidScope(statement, allowSwitch: true);

            case ContinueStatementSyntax:
                return !IsInsideValidScope(statement, allowSwitch: false);

            case IfStatementSyntax ifStatement:
                {
                    var thenTerm = AnalyzePossibleBlock(ifStatement.Statement, warnings);
                    var elseTerm = ifStatement.Else != null && AnalyzePossibleBlock(ifStatement.Else.Statement, warnings);

                    return thenTerm && elseTerm;
                }

            case WhileStatementSyntax whileStatement:
                {
                    bool loopTerminated = false;
                    var innerStatements = whileStatement.Statement is BlockSyntax block
                        ? block.Statements
                        : new SyntaxList<StatementSyntax>().Add(whileStatement.Statement);

                    foreach (var stmt in innerStatements)
                    {
                        if (loopTerminated)
                        {
                            var line = stmt.GetLocation()?.GetLineSpan().StartLinePosition.Line + 1 ?? -1;
                            warnings.Add($"[red]Unreachable code detected on line {line}[/]");
                            continue;
                        }

                        loopTerminated = AnalyzeStatement(stmt, warnings);
                    }
                    return false; // loops may or may not terminate here, so returning false is safer
                }

            case ForStatementSyntax forStatement:
                {
                    bool loopTerminated = false;
                    var innerStatements = forStatement.Statement is BlockSyntax block
                        ? block.Statements
                        : new SyntaxList<StatementSyntax>().Add(forStatement.Statement);

                    foreach (var stmt in innerStatements)
                    {
                        if (loopTerminated)
                        {
                            var line = stmt.GetLocation()?.GetLineSpan().StartLinePosition.Line + 1 ?? -1;
                            warnings.Add($"[red]Unreachable code detected on line {line}[/]");
                            continue;
                        }

                        loopTerminated = AnalyzeStatement(stmt, warnings);
                    }
                    return false;
                }

            case SwitchStatementSyntax switchStatement:
                {
                    bool allTerminated = true;
                    foreach (var section in switchStatement.Sections)
                    {
                        bool sectionTerminated = false;
                        foreach (var inner in section.Statements)
                        {
                            if (sectionTerminated)
                            {
                                var line = inner.GetLocation()?.GetLineSpan().StartLinePosition.Line + 1 ?? -1;
                                warnings.Add($"[red]Unreachable code detected on line {line}[/]");
                                continue;
                            }

                            sectionTerminated = AnalyzeStatement(inner, warnings);
                        }

                        if (!sectionTerminated)
                            allTerminated = false;
                    }

                    return allTerminated;
                }
        }

        return false;
    }


    private static bool IsInsideValidScope(SyntaxNode node, bool allowSwitch)
    {
        return node.Ancestors().Any(a =>
            a is ForStatementSyntax ||
            a is WhileStatementSyntax ||
            a is DoStatementSyntax ||
            (allowSwitch && a is SwitchStatementSyntax));
    }

    private static bool AnalyzePossibleBlock(StatementSyntax statement, List<string> warnings)
    {
        return statement switch
        {
            BlockSyntax block => AnalyzeBlock(block, warnings),
            _ => AnalyzeStatement(statement, warnings)
        };
    }
}