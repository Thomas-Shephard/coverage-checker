# Coverage Checker Command Line Tool

The Coverage Checker command line tool is a .NET tool for extracting code coverage metrics from various code coverage
formats.

For general information about the Coverage Checker project, see the repository's
main [README file](https://github.com/Thomas-Shephard/coverage-checker/blob/main/README.md).

## Installation

To install the Coverage Checker command line tool, install
the [CoverageChecker.CommandLine NuGet package](https://www.nuget.org/packages/CoverageChecker.CommandLine):

```
dotnet tool install --global CoverageChecker.CommandLine
```

## Usage

The CoverageChecker.CommandLine tool can be invoked by running `coveragechecker` from the command line.

| Option                     | Description                                                                   | Required | Default               |
|----------------------------|-------------------------------------------------------------------------------|----------|-----------------------|
| `-f`, `--format`           | The format of the coverage file(s). Options: `Auto`, `SonarQube`, `Cobertura` | No       | `Auto`                |
| `-d`, `--directory`        | The directory to search for the coverage file(s) within.                      | No       | The current directory |
| `-g`, `--glob-patterns`    | The glob pattern(s) to use to search for the coverage file(s).                | No       | `**/*.xml`            |
| `-i`, `--include`          | Glob patterns of files to include in the coverage analysis.                   | No       |                       |
| `-e`, `--exclude`          | Glob patterns of files to exclude from the coverage analysis.                 | No       |                       |
| `-l`, `--line-threshold`   | The line coverage threshold. Default: 80                                      | No       | 80                    |
| `-b`, `--branch-threshold` | The branch coverage threshold. Default: 80                                    | No       | 80                    |
| `-r`, `--rename-threshold` | The similarity threshold for rename detection (percentage). Default: 50       | No       | 50                    |
| `--delta`                  | Calculate coverage for changed lines only.                                    | No       | `false`               |
| `--delta-base`             | Base branch or commit to compare against for delta coverage.                  | No       | `origin/main`         |

The `--delta` and `--delta-base` options require Git to be installed and available on the system `PATH`.

## Examples

### Filtering Source Files

Only analyze source files in the `src` directory and exclude any generated files:

```bash
coveragechecker --include "src/**" --exclude "**/Generated/**"
```

### Analyzing Delta Coverage

Check coverage only for changed lines compared to the `develop` branch:

```bash
coveragechecker --delta --delta-base origin/develop
```

### Custom Thresholds and Search Patterns

Search for Cobertura files in a specific directory with custom coverage thresholds:

```bash
coveragechecker -d ./coverage-results -g "**/cobertura-coverage.xml" -l 90 -b 85
```

## Output

The CoverageChecker Command Line tool reads the specified coverage files and outputs the line and branch coverage of the analyzed files.
If the line or branch coverage is below the specified threshold, the tool will exit with a non-zero exit code.

### Coverage Gap Reporting

When thresholds are not met, the tool automatically identifies and reports the top 5 files with the most significant coverage gaps. This helps you quickly pinpoint where tests are missing.

For each problematic file, the output includes:
- **Uncovered Lines**: A summary of line ranges that have no coverage.
- **Partial Branches**: Detailed information about lines with branch coverage gaps (e.g., `Line 42 (1/2)` branches covered).

Example console output:
```text
[Line Coverage]: 75.00% (Threshold: 80.00%)
[Branch Coverage]: 60.00% (Threshold: 80.00%)

File Gaps: src/Services/AuthService.cs
  Uncovered Lines: 10-15, 22
  Partial Branches: Line 42 (1/2), Line 55 (0/2)
```

## GitHub Actions Integration

When running in a GitHub Actions environment (detected via the `GITHUB_ACTIONS` environment variable), the tool automatically enhances its output:

- **Workflow Commands**: Threshold failures and coverage gaps are reported as `::warning::` or `::error::` workflow commands.
- **File Annotations**: When thresholds fail, the tool emits warning annotations directly onto the changed lines in the Pull Request files view, highlighting missing line coverage and partial branch coverage.
- **Job Summary**: A detailed markdown summary is generated and attached to the workflow run, including:
  - An overall metric table with status indicators (✅/❌).
  - A delta coverage summary (if `--delta` is used).
  - A file breakdown table for the top 10 files with the lowest coverage, including a direct list of gaps.
