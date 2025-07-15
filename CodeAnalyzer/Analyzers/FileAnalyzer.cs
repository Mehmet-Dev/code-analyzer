using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalyzer.Analyzers;

public static class FileAnalyzer
{
    public static List<string> ShowFileStats(SyntaxNode root)
    {
        SyntaxTree tree = root.SyntaxTree;
        List<string> writes = new();

        // Part 1: file line count
        int fullLength = tree.GetLineSpan(root.Span).EndLinePosition.Line + 1;

        //Part 2: Number of methods
        IEnumerable<MethodDeclarationSyntax> methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
        int amountOfMethods = methods.Count();

        // Part 3: Longest method by line count & average method length
        var (methodName, lineCount, average) = GetLongestMethodAndAverageMethodLength(methods);

        // Part 4: Number of classes
        int amountOfClasses = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Count();

        // Part 5: Number of TODO/FIXME comments
        int amountOfPendingTasks = PendingTasksAnalyzer.CheckPendingTasks(root).Count();

        // Part 6: Number of properties/fields
        var (properties, fields) = GetAmountOfPropertiesAndFields(root);

        // Part 7: Calculate comment density
        double density = CalculateCommentDensity(root);

        writes.Add("[bold yellow]File wide stats[/]");
        writes.Add($"[green]Total lines:[/] {fullLength}");
        writes.Add($"[green]Number of methods:[/] {amountOfMethods}");
        writes.Add($"[green]Longest method:[/] {methodName} ([red]{lineCount} lines[/])");
        writes.Add($"[green]Average method length:[/] {average:F2} lines");
        writes.Add($"[green]Number of classes:[/] {amountOfClasses}");
        writes.Add($"[green]TODO/FIXME comments found:[/] {amountOfPendingTasks}");
        writes.Add($"[green]Properties count:[/] {properties}");
        writes.Add($"[green]Fields count:[/] {fields}");
        writes.Add($"[green]Comment density:[/] {density:F2}%");

        return writes;
    }

    /// <summary>
    /// Get the longest method and the average method length
    /// </summary>
    /// <param name="methods">Enumerator of methods (MethodDeclarationSyntax)</param>
    /// <returns>The method name and length of the longest method and the average method length</returns>
    private static (string methodName, int lineCount, double average) GetLongestMethodAndAverageMethodLength(IEnumerable<MethodDeclarationSyntax> methods)
    {
        Dictionary<string, int> linesCount = new();

        foreach (MethodDeclarationSyntax method in methods)
        {
            string methodName = method.Identifier.Text;
            int lineCount = method.SyntaxTree.GetLineSpan(method.Span).EndLinePosition.Line + 1;
            linesCount.Add(methodName, lineCount);
        }

        var highest = linesCount.MaxBy(kvp => kvp.Value);
        double average = linesCount.Values.ToArray().Average();

        return (highest.Key, highest.Value, average);
    }

    /// <summary>
    /// Get the amount of properties and fields
    /// </summary>
    /// <param name="root">Root node</param>
    /// <returns>The amount of properties and fields</returns>
    private static (int properties, int fields) GetAmountOfPropertiesAndFields(SyntaxNode root)
    {
        int properties = root.DescendantNodes().OfType<PropertyDeclarationSyntax>().Count();
        int fields = root.DescendantNodes().OfType<FieldDeclarationSyntax>().Count();

        return (properties, fields);
    }

    /// <summary>
    /// Calculate the comment density
    /// </summary>
    /// <param name="root"></param>
    /// <returns></returns>
    private static double CalculateCommentDensity(SyntaxNode root)
    {
        int commentLines = 0;
        int fullLines = 0;

        IEnumerable<SyntaxTrivia> comments = root.DescendantTrivia()
                        .Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia));

        // Determine how many lines of comments there are
        foreach (var comment in comments)
        {
            if (comment.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                commentLines++;
                continue;
            }

            var lineSpan = comment.SyntaxTree.GetLineSpan(comment.Span);
            var lineCount = lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line + 1;

            commentLines += lineCount;
        }

        var full = root.SyntaxTree.GetText();

        fullLines = full.Lines.Count > 0 ? full.Lines.Count : 1;

        return (double)commentLines / fullLines * 100;
    }
}