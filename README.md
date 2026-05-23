# Coverage Checker

[![Build, Test and Publish](https://github.com/Thomas-Shephard/coverage-checker/actions/workflows/build-test-and-publish.yml/badge.svg)](https://github.com/Thomas-Shephard/coverage-checker/actions/workflows/build-test-and-publish.yml)

Coverage Checker checks .NET code coverage from the command line or from a .NET library. 
Use it to enforce line and branch coverage thresholds, analyse changed lines in pull requests, and filter coverage to the source files that matter for your project.

## Features

- **Coverage formats**: Reads Cobertura and SonarQube reports. Cobertura reports with multiple sources are supported.
- **CLI and library APIs**: Use the global .NET tool in CI, or reference the library from custom build tooling.
- **Threshold enforcement**: Fails the CLI when configured coverage targets are not met.
- **Delta coverage**: Checks only changed lines against a Git base branch or commit.
- **File filtering**: Includes or excludes source files with glob patterns.
- **GitHub Actions output**: Emits workflow annotations and job summaries when running in GitHub Actions.
- **Integrated test workflow**: Runs tests and checks the generated coverage with one `run` command.

## Installation

Install the command line tool globally:

```bash
dotnet tool install --global CoverageChecker.CommandLine
```

Add the library package to a project:

```bash
dotnet add package CoverageChecker
```

## CLI Usage

Coverage Checker has two CLI commands:

- `check`: analyses existing coverage files. This is the default command.
- `run`: runs a command, substitutes `{output}` with a coverage output directory, then analyses the generated files.

Check existing Cobertura or SonarQube coverage files:

```bash
coveragechecker check --directory ./coverage --format Cobertura --glob-patterns "**/coverage.cobertura.xml" --line-threshold 80 --branch-threshold 70
```

Run tests and check the coverage they produce. The `{output}` token is replaced with a managed output directory:

```bash
coveragechecker run --command "dotnet test --collect 'XPlat Code Coverage' --results-directory {output}" --line-threshold 90
```

Check delta coverage against another branch:

```bash
coveragechecker --delta --delta-base origin/main --glob-patterns "**/coverage.cobertura.xml"
```

Limit analysis to selected source files:

```bash
coveragechecker --include "src/**" --exclude "**/Generated/**" --exclude "**/*Designer.cs"
```

See the [command line README](https://github.com/Thomas-Shephard/coverage-checker/blob/main/src/CoverageChecker.CommandLine) for the complete option reference.

## GitHub Actions

Install the tool, run tests, then check the generated coverage report. Use `fetch-depth: 0` when using `--delta`, because Coverage Checker needs Git history to compare against the base branch.

```yaml
name: Build and Test

on:
  pull_request:
    branches: [ main ]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v6
        with:
          fetch-depth: 0

      - uses: actions/setup-dotnet@v5

      - name: Install Coverage Checker
        run: dotnet tool install --global CoverageChecker.CommandLine

      - name: Test
        run: dotnet test --collect "XPlat Code Coverage" --results-directory ./coverage

      - name: Check coverage
        run: >
          coveragechecker
          --format Cobertura
          --glob-patterns "./coverage/**/coverage.cobertura.xml"
          --line-threshold 80
          --branch-threshold 70
          --delta
          --delta-base origin/main
```

In GitHub Actions, threshold failures and coverage gaps are emitted as workflow annotations, and a coverage summary is
added to the job summary.

## Important Behavior

- If coverage files are found but there are no applicable lines after parsing or filtering, line coverage is unavailable and the CLI fails.
- If no branches are present, branch coverage is reported as N/A and the branch threshold does not fail.
- For delta coverage, changed lines in files that appear in the coverage data must also be present as covered or
  uncovered line entries. If changed coverage files are found but none of their changed lines match the coverage data, the CLI fails.
- Delta coverage and rename detection require Git on the system `PATH`.
- Cobertura and SonarQube reports are supported. Cobertura reports with multiple source roots are supported.

## Library Usage

Use `CoverageAnalyser` when you need coverage metrics inside custom .NET tooling:

```csharp
using CoverageChecker;

CoverageAnalyserOptions options = new()
{
    CoverageFormat = CoverageFormat.Auto,
    Directory = "./coverage",
    GlobPatterns = ["**/coverage.cobertura.xml"]
};

CoverageAnalyser analyser = new(options);
Coverage coverage = analyser.AnalyseCoverage();

double lineCoverage = coverage.CalculateOverallCoverage(CoverageType.Line);
```

Package-specific READMEs contain the full CLI option reference and detailed library object model:

- [Coverage Checker Command Line Tool](https://github.com/Thomas-Shephard/coverage-checker/blob/main/src/CoverageChecker.CommandLine)
- [Coverage Checker Library](https://github.com/Thomas-Shephard/coverage-checker/blob/main/src/CoverageChecker)

## Contributions

Contributions are welcome! Read
the [contributing guide](https://github.com/Thomas-Shephard/coverage-checker/blob/main/CONTRIBUTING.md) to get started.

## License

This project is licensed under the MIT License. See
the [LICENSE](https://github.com/Thomas-Shephard/coverage-checker/blob/main/LICENSE) for details.
