# code-analyzer
C# code analyzer.

# 🛠️ Code Analyzer Roadmap

---

### 🧱 PHASE 1: Core Features (MVP)

| ✅ Feature                     | 🔍 What it does                                               |
|-------------------------------|---------------------------------------------------------------|
| ✅ Method length detection     | Warn when a method exceeds N lines                           |
| ✅ Parameter count check       | Flag methods with too many parameters (e.g., > 4)            |
| ✅ Magic number detection      | Find numeric literals that aren’t part of constants          |
| ✅ TODO/FIXME comment detector | Warn about unfinished code or dev notes left behind          |
| ✅ File-wide stats             | Total lines, number of methods, longest method, etc.         |
| ✅ Codebase restructure        | Organize files, split logic, improve naming and structure     |


---

### ⚙️ PHASE 2: Intermediate Features (Smarter Analysis)

| Feature                    | What it adds                                              |
|----------------------------|-----------------------------------------------------------|
| ⬜ Cyclomatic complexity estimator | Count decision points (if, switch, for, while) to show complexity |
| ⬜ Dead code detector (basic) | Warn if you detect code that never runs (e.g., after a return)  |
| ⬜ Duplicate string literals | Warn about repeated string literals that should be extracted |
| ⬜ Nested loop depth warning | Flag deeply nested loops (e.g., 3+ levels)                |
| ⬜ Method name clarity check | Warn about generic names like DoStuff, HandleIt, ProcessData |

---

### 📊 PHASE 3: Reporting + CLI Power-Ups

| Feature                              | What it does                                                                    |
| ------------------------------------ | ------------------------------------------------------------------------------- |
| ⬜ JSON report output                 | Export results to `results.json` for CI or external tools                       |
| ⬜ Markdown or HTML report            | Make nice-looking human-readable reports                                        |
| ⬜ Threshold configuration            | User can customize limits (e.g., max lines, param count) via CLI or config file |
| ⬜ File globbing                      | Analyze entire folders with `*.cs` pattern                                      |
| ⬜ Summary table with Spectre.Console | Show results in table format with colors (very cool looking)                    |
| ⬜ CLI flag-based check selection     | Run only specific checks via CLI flags (e.g., `--magic`, `--params`, etc.)      |


---

### 🧠 PHASE 4: Advanced Nerdy Stuff

| Feature                   | What it does                                              |
|---------------------------|-----------------------------------------------------------|
| ⬜ Roslyn Diagnostic integration | Turn your analyzer into a real Roslyn Analyzer that plugs into Visual Studio |
| ⬜ Symbol analysis         | Find unused variables, fields, or methods                  |
| ⬜ Basic type inference warnings | Detect redundant type declarations (e.g., int x = 5; vs var x = 5;) |
| ⬜ GitHub Actions integration | Runs analyzer on PRs and shows comments or fails builds  |
| ⬜ GUI frontend (optional) | WinForms / Avalonia / Web frontend to drag+drop .cs files for analysis |

---

### 🌱 Bonus Ideas / Easter Eggs (Fun Stuff)

| Idea                          | Why it's cool                                           |
|-------------------------------|---------------------------------------------------------|
| ⬜ Show random coding tip after analysis | Teach users while they run it                     |
| ⬜ “XP system” for files (give a "code health score") | Like a game: 85/100 points for clean code      |
| ⬜ ASCII art / intro banner using Spectre.Console | Just for flair 😎                                  |
| ⬜ Sarcastic mode             | Adds sassy or fun messages when code is bad ("Really? 8 parameters?") |

