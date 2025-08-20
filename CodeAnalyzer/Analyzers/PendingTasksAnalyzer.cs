using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeAnalyzer.Analyzers;

public static class PendingTasksAnalyzer
{
    /// <summary>
    /// Analyze a file for pending tasks
    /// </summary>
    /// <param name="root">Node root to analyze</param>
    /// <returns>A tuple where raw are the raw results used for json output and full is the nicely formatted results used for console logging</returns>
    public static (List<string> raw, List<string> full) Analyze(SyntaxNode root)
    {
        List<string> tasks = new();
        List<string> rawTasks = new();
        List<string> todoMarkers = new() // Markers to look out for to determine pending tasks
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
                        .Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia)); // Grab every comment 

        foreach (SyntaxTrivia comment in comments)
        {
            string text = comment.ToString().ToLower();

            string? match = todoMarkers.FirstOrDefault(m =>
                text.Contains(m, StringComparison.OrdinalIgnoreCase));
            if (match != null) // If a match is found
            {
                var lineNumber = comment.SyntaxTree.GetLineSpan(comment.Span).StartLinePosition.Line + 1;

                if (text.Trim().Length < 10) // If a task is vaguely explained add to the list
                {
                    tasks.Add($"[yellow]- {match.ToUpper()} on line {lineNumber} is vague, consider adding more detail.[/]");
                    rawTasks.Add($"{match.ToUpper()} on line {lineNumber} is vague, add more detail.");
                }
                else
                {
                    tasks.Add($"[red]- {match.ToUpper()} found on line {lineNumber}: [/][italic green]{text.Trim()}[/]");
                    rawTasks.Add($"{match.ToUpper()} found on line {lineNumber}");
                }

            }
        }

        return (rawTasks, tasks);
    }
}