# GitHub Copilot Instructions for Coverage Checker

You are an expert code reviewer and software engineer assisting with the `CoverageChecker` project. Your goal is to help write, review, and maintain high-quality, secure, and idiomatic C# code.

## 1. High Level Details
*   **Project:** C# .NET solution for parsing/analysing coverage reports (Cobertura, SonarQube) and enforcing CI/CD thresholds.
*   **Type:** CLI Tool (`CoverageChecker.CommandLine`) and Library (`CoverageChecker`).
*   **Frameworks:** .NET 8.0, .NET 9.0, .NET 10.0.
*   **Key Libraries:** `CommandLineParser`, `Microsoft.Extensions.FileSystemGlobbing`, `Microsoft.Extensions.Logging` (Source Gen), `NUnit`, `Moq`.

## 2. Build and Validate
Always verify changes using these commands.
*   **Build:** `dotnet build --configuration Release` (Ensures all targets build).
*   **Test:** `dotnet test --configuration Release --no-build --verbosity normal --collect:"XPlat Code Coverage" --results-directory ./coverage --settings coverlet.runsettings`
*   **Lint/Style:** Use `.editorconfig` rules. Build with `/warnaserror` when possible.

## 3. Project Layout
*   **`src/CoverageChecker/`**: Core library. Logic for parsing (`Parsers/`), services (`Services/`), and models (`Results/`).
*   **`src/CoverageChecker.CommandLine/`**: CLI entry point (`Program.cs`) and arguments (`CommandLineOptions.cs`).
*   **`tests/`**: Contains `Unit` (isolated) and `EndToEnd` (integration) test projects.

## 4. Coding & Architectural Standards
*   **Polymorphism over Conditionals:**
    *   **Strong Preference:** Use interfaces/abstract classes instead of `is`, `as`, `GetType()`, or enums to determine behavior.
    *   *Example:* `parser.Parse()` (polymorphic) is preferred over `if (type == Cobertura) ...`.
*   **Immutability:** Prefer `readonly` properties and immutable records for data models (e.g., `Coverage`, `FileCoverage`).
*   **Dependency Injection:** Use constructor injection. Avoid static state to ensure testability.
*   **Logging:** Use `Microsoft.Extensions.Logging` with **compile-time source generators** (`[LoggerMessage]`) for performance. Avoid standard `LogInformation`.
*   **Namespaces:** Use **file-scoped namespaces** (e.g., `namespace CoverageChecker;`).

## 5. Security Guidelines (Critical)
*   **XML Parsing:** PREVENT XXE. Always use `DtdProcessing = DtdProcessing.Ignore` (see `ParserBase.cs`).
*   **Timing Attacks:** Ensure security-critical comparisons are constant time.
*   **Input Validation:** Validate all external inputs (paths, glob patterns) to prevent traversal attacks.

## 6. Review Checklist
When reviewing code or suggesting changes, you **MUST** check for the following:

1.  **Documentation Updates (CRITICAL):**
    *   If changes were made to CLI arguments, public APIs, configuration logic, or core architecture:
    *   **Action:** Verify that ALL relevant documentation is updated. This includes `README.md`, `CONTRIBUTING.md`, XML documentation comments (`/// <summary>`), and these `copilot-instructions.md` themselves. If any documentation is missing or outdated, **explicitly flag this** in your review.
2.  **Violations of Polymorphism:** Flag usages of `GetType()` or unnecessary `switch` statements on types.
3.  **Security Leaks:** Identify logic that might introduce side-channel vulnerabilities (timing/error discrepancies) or XXE vectors.
4.  **Missing Tests:** Every new feature or bug fix **must** include NUnit tests. Mock external dependencies using Moq.
5.  **Inefficient Logging:** Suggest converting standard log calls to `[LoggerMessage]` source generators.
