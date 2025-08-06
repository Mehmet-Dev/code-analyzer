using CodeAnalyzer.Helpers.UserInfo;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Spectre.Console;

namespace CodeAnalyzer;

class Program
{
    static void Main(string[] args)
    {
        Console.Clear();
        AnalyzerCore core = new(args);

        core.Run();
    }
}