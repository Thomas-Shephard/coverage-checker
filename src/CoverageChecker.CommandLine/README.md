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

| Option                     | Description                                                           | Required | Default               |
|----------------------------|-----------------------------------------------------------------------|----------|-----------------------|
| `-f`, `--format`           | The format of the coverage file(s). Options: `SonarQube`, `Cobertura` | Yes      |                       |
| `-d`, `--directory`        | The directory to search for the coverage file(s) within.              | No       | The current directory |
| `-g`, `--glob-patterns`    | The glob pattern(s) to use to search for the coverage file(s).        | No       | `*.xml`               |
| `-l`, `--line-threshold`   | The line coverage threshold. Default: 80                              | No       | 80                    |
| `-b`, `--branch-threshold` | The branch coverage threshold. Default: 80                            | No       | 80                    |
| `--delta`                  | Calculate coverage for changed lines only.                            | No       | `false`               |
| `--delta-base`             | Base branch or commit to compare against for delta coverage.          | No       | `origin/main`         |

## Output

The CoverageChecker Command Line tool reads the specified coverage files and outputs the line and branch coverage of the analyzed files.
If the line or branch coverage is below the specified threshold, the tool will exit with a non-zero exit code.

## GitHub Actions Integration

When running in a GitHub Actions environment, the tool automatically enhances its output:

- **Workflow Commands**: Coverage results and threshold failures are reported as `::notice::`, `::warning::`, or `::error::` workflow commands, making them visible directly in the GitHub Actions UI and pull request files view.
- **Job Summary**: A detailed markdown summary is generated and attached to the workflow run, including an overall metric table and a breakdown of the top 10 files with the lowest coverage.