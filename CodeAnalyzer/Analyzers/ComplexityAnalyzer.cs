using CodeAnalyzer.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalyzer.Analyzers;

/// <summary>
/// This determines the complexity of your code by checking how many decision points and loops there are for each method.
/// It uses an arbitrary value to determine how complex your code is.
/// </summary>
public static class ComplexityAnalyzer
{
    /// <summary>
    /// Analyze your code for complexity
    /// </summary>
    /// <param name="root">Root node of the file</param>
    /// <returns>A dictionary used for bare json output, and a List of strings used to display the results</returns>
    public static (Dictionary<string, ComplexityAnalysis> analysis, List<string> printing) Analyze(SyntaxNode root)
    {
        List<string> linesToPrint = new(); // Used for displaying the results
        Dictionary<string, ComplexityAnalysis> raw = new(); // Used for JSON output

        IEnumerable<MethodDeclarationSyntax> methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>(); // Get all methods

        foreach (MethodDeclarationSyntax method in methods)
        {
            int total = 1; // Base complexity value of a method
            List<string> strings = new();
            ComplexityAnalysis a = new(); // Used for JSON output

            // Part 1: determine amount of if statements
            int ifStatements = CalculateIfStatements(method);
            if (ifStatements > 0)
            {
                strings.Add($"[darkgreen]- If and ElseIf statements: {ifStatements}[/]");
                total += ifStatements;
            }

            a.IfStatements = ifStatements;

            // Part 2: Determine for and foreach statements
            int forStatements = CalculateForAndForeach(method);
            if (forStatements > 0)
            {
                strings.Add($"[deepskyblue1]- For and ForEach loops: {forStatements}[/]");
                total += forStatements;
            }

            a.ForStatements = forStatements;

            // Part 3: Determine while and do statements
            int whileAndDoStatements = CalculateWhileAndDoStatements(method);
            if (whileAndDoStatements > 0)
            {
                strings.Add($"[lightseagreen]- While and Do loops: {whileAndDoStatements}[/]");
                total += whileAndDoStatements;
            }

            a.WhileAndDoStatements = whileAndDoStatements;

            // Part 4: Determine switch blocks
            int switchStatements = CalculateSwitchBlocks(method);
            if (switchStatements > 0)
            {
                strings.Add($"[mediumorchid]- Switch cases: {switchStatements}[/]");
                total += switchStatements;
            }

            a.SwitchStatements = switchStatements;

            // Part 5: && and ||
            int logicOperators = CalculateLogicOperators(method);
            if (logicOperators > 0)
            {
                strings.Add($"[orange3]- Logic operators (&&, ||): {logicOperators}[/]");
                total += logicOperators;
            }

            a.LogicOperators = logicOperators;

            string complexity = DetermineComplexity(total, method.Identifier.Text);
            linesToPrint.Add(complexity);
            foreach (string s in strings)
                linesToPrint.Add(s);

            a.TotalComplexity = total;
            raw.Add(method.Identifier.Text, a);
        }

        // After everything, explain the arbitrary values
        linesToPrint.Add("\n[bold underline yellow]Complexity Level Explanation[/]");
        linesToPrint.Add("[green]1–5   → Easy[/] — Simple, clear control flow.");
        linesToPrint.Add("[yellow]6–10 → Moderate[/] — Some branching or nesting.");
        linesToPrint.Add("[red]11–20 → Complex[/] — Consider splitting or simplifying.");
        linesToPrint.Add("[bold red]>20   → Critical[/] — Refactor if possible.");

        linesToPrint.Add("\n[italic dim]Note:[/] [italic]High complexity isn't always bad — sometimes you're forced into it (e.g. performance constraints, legacy systems, or third-party code). Just make sure it's [bold]intentional[/], not accidental.[/]");

        return (raw, linesToPrint);
    }

    /// <summary>
    /// Determine complexity of a method by adding everything up
    /// This does like the same work twice but yea I took a shortcut
    /// </summary>
    /// <param name="root">Root node of file</param>
    /// <returns>An integer of everything added up</returns>
    public static int CheckFileComplexity(SyntaxNode root)
    {
        IEnumerable<MethodDeclarationSyntax> methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
        int complexity = 1;

        foreach (MethodDeclarationSyntax method in methods)
        {
            complexity += CalculateIfStatements(method)
                        + CalculateForAndForeach(method)
                        + CalculateLogicOperators(method)
                        + CalculateSwitchBlocks(method)
                        + CalculateWhileAndDoStatements(method);
        }

        return complexity;
    }

    /// <summary>
    /// Determine amount of if statements in a method
    /// </summary>
    /// <param name="method">The method to analyze</param>
    /// <returns>Number of if statements</returns>
    private static int CalculateIfStatements(MethodDeclarationSyntax method)
        => method.DescendantNodes().OfType<IfStatementSyntax>().Count();

    /// <summary>
    /// Determine amount of for and foreach loops in a method
    /// </summary>
    /// <param name="method">The method to analyze</param>
    /// <returns>Number of for and foreach loops</returns>
    private static int CalculateForAndForeach(MethodDeclarationSyntax method)
        => method.DescendantNodes().OfType<ForEachStatementSyntax>().Count()
            + method.DescendantNodes().OfType<ForStatementSyntax>().Count();

    /// <summary>
    /// Determine amount of while and do-while loops in a method
    /// </summary>
    /// <param name="method">The method to analyze</param>
    /// <returns>Number of while and do-while loops</returns>
    private static int CalculateWhileAndDoStatements(MethodDeclarationSyntax method)
        => method.DescendantNodes().OfType<WhileStatementSyntax>().Count()
            + method.DescendantNodes().OfType<DoStatementSyntax>().Count();

    /// <summary>
    /// Determine amount of logical operators (&&, ||) in a method
    /// </summary>
    /// <param name="method">The method to analyze</param>
    /// <returns>Number of logical operators</returns>
    private static int CalculateLogicOperators(MethodDeclarationSyntax method)
        => method.DescendantTokens().Count(t => t.IsKind(SyntaxKind.AmpersandAmpersandToken)
                                        || t.IsKind(SyntaxKind.BarBarToken));
    
    /// <summary>
    /// Determine amount of switch blocks 
    /// </summary>
    /// <param name="method">Method to analyze</param>
    /// <returns>Number of switch blocks</returns>
    private static int CalculateSwitchBlocks(MethodDeclarationSyntax method)
    {
        var switches = method.DescendantNodes().OfType<SwitchStatementSyntax>();
        int totalCases = 0;

        foreach (var switchStatement in switches)
        {
            totalCases += switchStatement.Sections
                .SelectMany(sec => sec.Labels)
                .Count(label => label is CaseSwitchLabelSyntax);
        }

        return totalCases;
    }

    /// <summary>
    /// Make a nice string that can be used with <c>AnsiConsole</c>
    /// </summary>
    /// <param name="total"></param>
    /// <param name="methodName"></param>
    /// <returns></returns>
    private static string DetermineComplexity(int total, string methodName)
    {
        if (total > 20)
            return $"[bold red]{methodName} - Complexity: {total} (Critical)[/]";
        else if (total >= 11)
            return $"[red]{methodName} - Complexity: {total} (Complex)[/]";
        else if (total >= 6)
            return $"[yellow]{methodName} - Complexity: {total} (Moderate)[/]";
        else
            return $"[green]{methodName} - Complexity: {total} (Easy)[/]";
    }
}