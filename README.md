# Coverage Checker

[![Build, Test and Publish](https://github.com/Thomas-Shephard/coverage-checker/actions/workflows/build-test-and-publish.yml/badge.svg)](https://github.com/Thomas-Shephard/coverage-checker/actions/workflows/build-test-and-publish.yml)

Allows for the checking of code coverage using the provided GitHub action or NuGet package.

## GitHub Action

### Usage

The code coverage must already have been generated and saved in a file. The action will then parse the coverage file and check the coverage. If desired, the action will fail if the coverage is below the specified threshold.

The action supports the following coverage file formats:

- SonarQube
- Cobertura

```yaml
- name: Check coverage
  id: check-coverage
  uses: Thomas-Shephard/coverage-checker@v0.2.0
  with:
    format: 'cobertura'
    directory: 'coverage'
    glob: '**/coverage.cobertura.xml'
    line-threshold: 80
    branch-threshold: 80
    fail-if-below-threshold: true
```

### Inputs
| Name                      | Description                                                        | Required | Default               |
|---------------------------|--------------------------------------------------------------------|----------|-----------------------|
| `format`                  | The format of the coverage file. Options: `sonarqube`, `cobertura` | Yes      |                       |
| `directory`               | The directory containing the coverage file(s).                     | No       | The current directory |
| `glob-pattern`            | The glob pattern to match the coverage file(s).                    | Yes      |                       |
| `line-threshold`          | The line coverage threshold.                                       | No       | 80                    |
| `branch-threshold`        | The branch coverage threshold.                                     | No       | 80                    |
| `fail-if-below-threshold` | Whether to fail the action if the coverage is below the threshold. | No       | true                  |
| `fail-if-no-files-found`  | Whether to fail the action if no coverage files are found.         | No       | true                  |

### Outputs
| Name              | Description                         |
|-------------------|-------------------------------------|
| `line-coverage`   | The line coverage of the project.   |
| `branch-coverage` | The branch coverage of the project. |

These outputs can be used in subsequent steps, as shown below:

```yaml
- name: Use coverage results
  run: |
    echo "Line coverage ${{ steps.check-coverage.outputs.line-coverage }}"
    echo "Branch coverage ${{ steps.check-coverage.outputs.branch-coverage }}"
```

## NuGet Package

### Installation

Install the CoverageChecker NuGet package using the .NET CLI:

```
dotnet add package CoverageChecker
```

### Features

#### Supported Coverage Formats

The following coverage formats are supported:
 * [Cobertura](https://github.com/cobertura/web/blob/master/htdocs/xml/coverage-04.dtd) - `CoverageFormat.Cobertura`
 * [SonarQube](https://docs.sonarsource.com/sonarqube/latest/analyzing-source-code/test-coverage/generic-test-data/) - `CoverageFormat.SonarQube`

#### Coverage Analysis

The coverage analyser can be used to check the coverage of a project; both line and branch coverage are supported. Coverage can be calculated based on package, file, class, method, line, etc.

##### Code Example

```csharp
using CoverageChecker;
using CoverageChecker.Results;

const CoverageFormat coverageFormat = CoverageFormat.Cobertura;
const string directory = @"file-directory";
const string globPattern = "*.xml";

CoverageAnalyser analyser = new(coverageFormat, directory, globPattern);
Coverage coverage = analyser.AnalyseCoverage();

if (coverage.Files.Count is 0) {
    Console.WriteLine("No coverage files found");
    return;
}

foreach (FileCoverage fileCoverage in coverage.Files) {
    Console.WriteLine($"Package {fileCoverage.PackageName ?? ""}: File={fileCoverage.Path} Lines={fileCoverage.Lines.Count}");

    foreach (IGrouping<string?, LineCoverage> classCoverage in fileCoverage.Lines.GroupBy(line => line.ClassName)) {
        Console.WriteLine($"\tClass {classCoverage.Key ?? ""}");

        foreach (LineCoverage line in classCoverage.OrderBy(line => line.LineNumber)) {
            Console.WriteLine(line.Branches is null
                                  ? $"\t\tLine {line.LineNumber}: Covered={(line.IsCovered ? "Yes" : "No")}"
                                  : $"\t\tLine {line.LineNumber}: Covered={(line.IsCovered ? "Yes" : "No")}, Branches=({line.CoveredBranches}/{line.Branches})");
        }
    }
}

Console.WriteLine($"Total line coverage: {coverage.CalculateOverallCoverage():P1}");
Console.WriteLine($"Total branch coverage: {coverage.CalculateOverallCoverage(CoverageType.Branch):P1}");

```

## Contributions

Contributions are welcome! Read the [contributing guide](CONTRIBUTING.md) to get started.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) for details.