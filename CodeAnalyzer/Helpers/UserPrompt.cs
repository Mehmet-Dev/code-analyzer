using Spectre.Console;

namespace CodeAnalyzer.Helpers.UserInfo;

static class UserPrompt
{
    public static int AskComplexityLevel()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
            .Title("Is your code complexity such that longer methods are justified? (Choose one. Now.)")
            .AddChoices(new[]
            {
                "1 - No, keep methods short",
                "2 - Somewhat complex, occasional long methods allowed",
                "3 - Yes, code is quite complex",
                "4 - Very complex or legacy code"
            })
        );

        return int.Parse(choice.Substring(0, 1));
    }
}