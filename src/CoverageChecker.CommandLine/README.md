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

## GitHub Actions Integration

When running in a GitHub Actions environment, the tool automatically enhances its output:

- **Workflow Commands**: Coverage results and threshold failures are reported as `::notice::`, `::warning::`, or `::error::` workflow commands, making them visible directly in the GitHub Actions UI and pull request files view.
- **Job Summary**: A detailed markdown summary is generated and attached to the workflow run, including an overall metric table and a breakdown of the top 10 files with the lowest coverage.