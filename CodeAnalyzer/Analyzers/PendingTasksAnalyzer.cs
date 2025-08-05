using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeAnalyzer.Analyzers;

public static class PendingTasksAnalyzer
{
    public static List<string> Analyze(SyntaxNode root)
    {
        List<string> tasks = new();
        List<string> todoMarkers = new()
        {
            "todo",
            "fixme",
            "hack",
            "xxx",       // often used as a "this needs attention" tag
            "bug",
            "note",      // sometimes used for reminders
            "tbd",       // to be decided
            "fix",       // shorthand some people use
            "optimize",  // performance-related TODOs
            "cleanup"    // used for code refactoring notes
        };

        IEnumerable<SyntaxTrivia> comments = root.DescendantTrivia()
                        .Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia));

        foreach (SyntaxTrivia comment in comments)
        {
            string text = comment.ToString().ToLower();

            string? match = todoMarkers.FirstOrDefault(m =>
                text.Contains(m, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                var lineNumber = comment.SyntaxTree.GetLineSpan(comment.Span).StartLinePosition.Line + 1;

                if (text.Trim().Length < 10)
                    tasks.Add($"[yellow]- {match.ToUpper()} on line {lineNumber} is vague, consider adding more detail.[/]");
                else
                    tasks.Add($"[red]- {match.ToUpper()} found on line {lineNumber}: [/][italic green]{text.Trim()}[/]");
            }
        }

        return tasks;
    }
}