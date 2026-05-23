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

The CoverageChecker.CommandLine tool supports two commands: `check` (default) and `run`.

### `check` Command

Analyzes existing coverage files. This is the default command if no command is specified.

```bash
coveragechecker check [options]
# or simply
coveragechecker [options]
```

### `run` Command

Runs a specified command (e.g., your test runner), captures the coverage results, and then performs the analysis. It automatically handles temporary directory creation and cleanup.

```bash
coveragechecker run --command "dotnet test --collect 'XPlat Code Coverage' --results-directory {output}" [options]
```

### Common Options (Base)

These options apply to both `check` and `run` commands.

| Option                     | Description                                                                                | Required | Default       |
|----------------------------|--------------------------------------------------------------------------------------------|----------|---------------|
| `-f`, `--format`           | The format of the coverage file(s). Options: `Auto`, `SonarQube`, `Cobertura`, `OpenCover` | No       | `Auto`        |
| `-g`, `--glob-patterns`    | The glob pattern(s) to use to search for the coverage file(s).                             | No       | `**/*.xml`    |
| `-i`, `--include`          | Glob patterns of files to include in the coverage analysis.                                | No       |               |
| `-e`, `--exclude`          | Glob patterns of files to exclude from the coverage analysis.                              | No       |               |
| `-l`, `--line-threshold`   | The line coverage threshold. Default: 80                                                   | No       | 80            |
| `-b`, `--branch-threshold` | The branch coverage threshold. Default: 80                                                 | No       | 80            |
| `--delta-line-threshold`   | The delta line coverage threshold. Defaults to `--line-threshold`.                         | No       |               |
| `--delta-branch-threshold` | The delta branch coverage threshold. Defaults to `--branch-threshold`.                     | No       |               |
| `--rename-threshold`       | The similarity threshold for rename detection (percentage). Default: 50                    | No       | 50            |
| `--delta`                  | Calculate coverage for changed lines only.                                                 | No       | `false`       |
| `--strict-delta`           | Fail when Git-changed files with changed lines are absent from coverage data.              | No       | `false`       |
| `--delta-base`             | Base branch or commit to compare against for delta coverage.                               | No       | `origin/main` |

### `check` Specific Options

| Option              | Description                                               | Required | Default               |
|---------------------|-----------------------------------------------------------|----------|-----------------------|
| `-d`, `--directory` | The directory to search for the coverage file(s) within.  | No       | The current directory |

### `run` Specific Options

| Option                  | Description                                                                                           | Required | Default           |
|-------------------------|-------------------------------------------------------------------------------------------------------|----------|-------------------|
| `-c`, `--command`       | The command to execute. Use `{output}` as a placeholder for the results directory.                    | Yes      |                   |
| `-o`, `--output`        | The directory where coverage results will be stored. If not specified, a temporary directory is used. | No       |                   |
| `--working-directory`   | The directory where the command is executed. Relative paths are resolved from the current directory.  | No       | Current directory |
| `-t`, `--timeout`       | The maximum amount of time, in minutes, that the specified command is allowed to run.                 | No       | 30                |
| `--continue-on-failure` | Continue with coverage analysis even if the command fails.                                            | No       | `false`           |

The `--delta` and `--delta-base` options require Git to be installed and available on the system `PATH`.

## Examples

### Integrated Workflow (Run & Check)

Run tests and check coverage in a single command. The `{output}` placeholder will be replaced with a managed temporary directory that is automatically cleaned up after analysis.

```bash
coveragechecker run --command "dotnet test --collect 'XPlat Code Coverage' --results-directory {output}" --line-threshold 90
```

Run tests from a project or solution subdirectory while invoking Coverage Checker from a repository root:

```bash
coveragechecker run --working-directory ./src/MySolution --command "dotnet test --collect 'XPlat Code Coverage' --results-directory {output}"
```

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

Use different thresholds for overall coverage and changed lines:

```bash
coveragechecker --delta --line-threshold 80 --delta-line-threshold 100
```

Fail when a changed file is not represented in the coverage report at all:

```bash
coveragechecker --delta --strict-delta --delta-base origin/develop
```

Apply strict delta to the same source-file scope used by coverage analysis:

```bash
coveragechecker --delta --strict-delta --include "src/**/*.cs" --exclude "**/*.Generated.cs"
```

### Custom Thresholds and Search Patterns

Search for Cobertura files in a specific directory with custom coverage thresholds:

```bash
coveragechecker -d ./coverage-results -g "**/cobertura-coverage.xml" -l 90 -b 85
```

## Output

The CoverageChecker Command Line tool reads the specified coverage files and outputs the line and branch coverage of the analyzed files.
If the line or branch coverage is below the specified threshold, the tool will exit with a non-zero exit code.
If coverage files are found but no applicable lines remain after parsing or include/exclude filtering, line coverage is reported as unavailable and the tool exits with a non-zero exit code.
Branch coverage with no branches is reported as unavailable and does not fail the branch threshold.
Delta coverage uses `--delta-line-threshold` and `--delta-branch-threshold` when provided. If either delta threshold is omitted, it defaults to the corresponding overall threshold.
For delta coverage, changed lines in files that appear in the coverage data must match covered line entries; if changed coverage files are found but none of their changed lines are matched, the tool exits with a non-zero exit code.
Without `--strict-delta`, changed files that are completely absent from coverage data are ignored so docs and config-only changes remain compatible with existing workflows.
With `--strict-delta`, Git-changed files with changed line numbers must be present in the coverage data. If `--include` or `--exclude` is provided, the strict missing-file check respects that same scope. Otherwise, strict delta applies to every file reported by Git with changed line numbers without distinguishing source from non-source files.

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
