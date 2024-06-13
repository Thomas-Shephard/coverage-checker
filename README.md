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

## NuGet Package

### Installation

Install the CoverageChecker NuGet package using the .NET CLI:

```
dotnet add package CoverageChecker
```

### Features

#### Coverage File Parsing

Multiple coverage file formats are supported, as shown below:

| Format                                                                                                            | Class                                   |
|-------------------------------------------------------------------------------------------------------------------|-----------------------------------------|
| [SonarQube](https://docs.sonarsource.com/sonarqube/latest/analyzing-source-code/test-coverage/generic-test-data/) | CoverageChecker.Parsers.SonarQubeParser |
| [Cobertura](https://github.com/cobertura/web/blob/master/htdocs/xml/coverage-04.dtd)                              | CoverageChecker.Parsers.CoberturaParser |

#### Coverage Checking

The coverage checker can be used to check the coverage of a project; both line and branch coverage are supported. Coverage can be calculated based on package, file, class, method, line, etc.

##### Code Example

```csharp
using CoverageChecker;
using CoverageChecker.Parsers;
using CoverageChecker.Results;

const string directory = @"file-directory";
const string globPattern = "*.xml";

CoberturaParser parser = new(directory, globPattern);
Coverage coverage = parser.LoadCoverage();

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