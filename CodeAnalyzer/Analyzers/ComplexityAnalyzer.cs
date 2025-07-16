using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalyzer.Analyzers;

public static class ComplexityAnalyzer
{
    public static List<string> CalculateComplexity(SyntaxNode root)
    {
        List<string> linesToPrint = new();

        IEnumerable<MethodDeclarationSyntax> methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (MethodDeclarationSyntax method in methods)
        {
            int total = 1;
            List<string> strings = new();

            // Part 1: determine amount of if statements
            int ifStatements = CalculateIfStatements(method);
            if (ifStatements > 0)
            {
                strings.Add($"[darkgreen]- If and ElseIf statements: {ifStatements}[/]");
                total += ifStatements;
            }

            // Part 2: Determine for and foreach statements
            int forStatements = CalculateForAndForeach(method);
            if (forStatements > 0)
            {
                strings.Add($"[deepskyblue1]- For and ForEach loops: {forStatements}[/]");
                total += forStatements;
            }

            // Part 3: Determine while and do statements
            int whileAndDoStatements = CalculateWhileAndDoStatements(method);
            if (whileAndDoStatements > 0)
            {
                strings.Add($"[lightseagreen]- While and Do loops: {whileAndDoStatements}[/]");
                total += whileAndDoStatements;
            }

            // Part 4: Determine switch blocks
            int switchStatements = CalculateSwitchBlocks(method);
            if (switchStatements > 0)
            {
                strings.Add($"[mediumorchid]- Switch cases: {switchStatements}[/]");
                total += switchStatements;
            }

            // Part 5: && and ||
            int logicOperators = CalculateLogicOperators(method);
            if (logicOperators > 0)
            {
                strings.Add($"[orange3]- Logic operators (&&, ||): {logicOperators}[/]");
                total += logicOperators;
            }

            string complexity = DetermineComplexity(total, method.Identifier.Text);
            linesToPrint.Add(complexity);
            foreach (string s in strings)
                linesToPrint.Add(s);
        }

        linesToPrint.Add("\n[bold underline yellow]Complexity Level Explanation[/]");
        linesToPrint.Add("[green]1–5   → Easy[/] — Simple, clear control flow.");
        linesToPrint.Add("[yellow]6–10 → Moderate[/] — Some branching or nesting.");
        linesToPrint.Add("[red]11–20 → Complex[/] — Consider splitting or simplifying.");
        linesToPrint.Add("[bold red]>20   → Critical[/] — Refactor if possible.");

        linesToPrint.Add("\n[italic dim]Note:[/] [italic]High complexity isn't always bad — sometimes you're forced into it (e.g. performance constraints, legacy systems, or third-party code). Just make sure it's [bold]intentional[/], not accidental.[/]");

        return linesToPrint;
    }

    private static int CalculateIfStatements(MethodDeclarationSyntax method)
        => method.DescendantNodes().OfType<IfStatementSyntax>().Count();

    private static int CalculateForAndForeach(MethodDeclarationSyntax method)
        => method.DescendantNodes().OfType<ForEachStatementSyntax>().Count()
            + method.DescendantNodes().OfType<ForStatementSyntax>().Count();

    private static int CalculateWhileAndDoStatements(MethodDeclarationSyntax method)
        => method.DescendantNodes().OfType<WhileStatementSyntax>().Count()
            + method.DescendantNodes().OfType<DoStatementSyntax>().Count();

    private static int CalculateLogicOperators(MethodDeclarationSyntax method)
        => method.DescendantTokens().Count(t => t.IsKind(SyntaxKind.AmpersandAmpersandToken)
                                        || t.IsKind(SyntaxKind.BarBarToken));


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