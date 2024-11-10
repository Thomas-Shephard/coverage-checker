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

| Option                  | Description                                                           | Required | Default               |
|-------------------------|-----------------------------------------------------------------------|----------|-----------------------|
| `-f`, `--format`        | The format of the coverage file(s). Options: `SonarQube`, `Cobertura` | Yes      |                       |
| `-d`, `--directory`     | The directory to search for the coverage file(s) within.              | No       | The current directory |
| `-g`, `--glob-patterns` | The glob pattern(s) to use to search for the coverage file(s).        | No       | `*.xml`               |

## Output

The CoverageChecker Command Line tool will output both the line and branch coverage of the analyzed files. 