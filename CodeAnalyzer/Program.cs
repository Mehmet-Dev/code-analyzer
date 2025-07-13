using CodeAnalyzer.Helpers.UserInfo;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalyzer;

class Program
{
    static void Main(string[] args)
    {
        Console.Clear();
        if (args.Length == 0)
        {
            Console.WriteLine("Please provide a C# file path to analyze.");
            return;
        }

        var filePath = args[0];

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"File '{filePath}' not found.");
            return;
        }

        int lengthTreshold = UserPrompt.AskComplexityLevel() switch
        {
            1 => 20,
            2 => 50,
            3 => 100,
            4 => 150,
            _ => 20
        };

        var root = Analyzer.ReturnRoot(filePath);
        IEnumerable<MethodDeclarationSyntax> methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        Analyzer.CheckMethodLengths(methods, lengthTreshold);
        Console.Clear();
        Analyzer.CheckParameterCount(methods);
        Console.Clear();
        Analyzer.CheckMagicNumbers(methods);
        Console.Clear();
        Analyzer.CheckUnchangedCode(root);
    }
}