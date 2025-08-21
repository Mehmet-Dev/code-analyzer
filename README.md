---

# Code Analyzer

A lightweight **C# code analysis tool** using Roslyn designed to help developers maintain cleaner, more maintainable codebases.

This tool scans C# files for common issues such as overly complex methods, excessive parameters, magic numbers, dead code, and more. It provides both **human-readable console output** and **JSON reports** for CI/CD integration.

---

## Features

* **Method Length Detection** – Warns when methods exceed a defined threshold.
* **Parameter Count Check** – Flags methods with too many parameters.
* **Magic Number Detection** – Identifies numeric literals that should be constants.
* **TODO/FIXME Comment Finder** – Detects unfinished or temporary code.
* **File Statistics** – Summaries including line count, comment ratio, and method count.
* **Complexity Estimation** – Scores methods based on branching logic.
* **Dead Code Detection** – Finds unreachable statements.
* **Duplicate String Warnings** – Detects repeated string literals.
* **Nested Loop Depth Check** – Flags methods with excessive nesting.
* **Generic Method Name Check** – Warns against vague names like `DoStuff` or `HandleIt`.
* **JSON Output Option** – Useful for automation and CI pipelines.

---

## Installation

1. Clone the repository:

   ```
   git clone https://github.com/yourusername/code-analyzer.git
   cd code-analyzer
   ```

2. Build the project:

   ```
   dotnet build
   ```

3. Move up into the CodeAnalyzer folder

   ```
   cd CodeAnalyzer
   ```

4. Run the analyzer:
    ```
    dotnet run -- <path-to-file.cs> <flags>
    ```

---

## Usage

Example:

```
dotnet run -- Sample.cs --methodLength --params --magic
```

Available flags:

* `--methodLength` – Check method size.
* `--params` – Check parameter counts.
* `--magic` – Detect magic numbers.
* `--pending` – Find TODO/FIXME comments.
* `--fileStats` – Show file statistics.
* `--methodComplexity` – Estimate method complexity.
* `--deadCode` – Detect unreachable code.
* `--duplicate` – Warn about duplicate strings.
* `--methodDepth` – Check loop nesting depth.
* `--genericNames` – Warn about vague method names.

---

## Output Modes

### Standard Output

By default, results are displayed in a **colorized console output**, highlighting warnings and analysis results clearly.

---

### JSON Output

Use the `--json` flag to generate a **machine-readable JSON report**, perfect for integrating into CI/CD pipelines or other automated workflows.

Example:

```
dotnet run -- Sample.cs --json
```

---

### Bulk Analysis

Analyze multiple files at once using the `--bulk` flag.

Example:

```
dotnet run -- --bulk src/**/*.cs
```

This will scan all matching files and provide a **combined report**.

---

### Bulk JSON Analysis

Combine `--bulk` with `--json` to produce a **comprehensive JSON report** for an entire codebase.

Example:

```
dotnet run -- --bulk src/**/*.cs --json
```

This generates a structured JSON file containing results for all analyzed files.

---

## License

MIT License – feel free to use, modify, and share.

---