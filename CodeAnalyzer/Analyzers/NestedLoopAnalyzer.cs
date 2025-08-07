using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalyzer.Analyzers;

public static class NestedLoopAnalyzer
{
    public static Dictionary<string, int> Analyze(SyntaxNode root, int threshold)
    {
        var results = new Dictionary<string, int>();
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            if (method.Body != null)
            {
                int depth = AnalyzeBlock(method.Body, 0);
                if (depth > threshold)
                {
                    results[method.Identifier.Text] = depth;
                }
            }
        }

        return results;
    }

    private static int AnalyzeBlock(StatementSyntax statement, int depth)
    {
        int maxDepth = depth;

        if (statement is BlockSyntax block)
        {
            foreach (var stmt in block.Statements)
            {
                int childDepth = AnalyzeBlock(stmt, depth);
                maxDepth = Math.Max(maxDepth, childDepth);
            }
            return maxDepth;
        }

        if (statement is ForStatementSyntax or WhileStatementSyntax or ForEachStatementSyntax or DoStatementSyntax)
        {
            int newDepth = depth + 1;
            var loopBody = GetLoopBody(statement);

            if (loopBody != null)
            {
                int bodyDepth = AnalyzeBlock(loopBody, newDepth);
                maxDepth = Math.Max(maxDepth, bodyDepth);
            }
            else
            {
                maxDepth = Math.Max(maxDepth, newDepth);
            }
            return maxDepth;
        }

        return maxDepth;
    }


    private static StatementSyntax? GetLoopBody(StatementSyntax statement)
    {
        return statement switch
        {
            ForStatementSyntax forStmt => forStmt.Statement,
            WhileStatementSyntax whileStmt => whileStmt.Statement,
            ForEachStatementSyntax foreachStmt => foreachStmt.Statement,
            DoStatementSyntax doStmt => doStmt.Statement,
            _ => null
        };
    }
}
