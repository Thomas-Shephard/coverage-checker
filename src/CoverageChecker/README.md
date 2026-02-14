# Coverage Checker Library

The Coverage Checker library is a .NET library for extracting code coverage metrics from various code coverage formats.
It is intended to be used as a development dependency if you need to extract code coverage metrics as part of a .NET
project.

For general information about the Coverage Checker project, see the repository's
main [README file](https://github.com/Thomas-Shephard/coverage-checker/blob/main/README.md).

## Installation

To use the Coverage Checker library in your .NET project, add
the [CoverageChecker NuGet package](https://www.nuget.org/packages/CoverageChecker) to your project:

```
dotnet add package CoverageChecker
```

## Usage

The `CoverageAnalyser` class is the entry point for extracting code coverage metrics from a coverage file.

The following example shows how to use the `CoverageAnalyser` class to extract code coverage metrics from a coverage
file while automatically detecting its format:

```csharp
using CoverageChecker;

CoverageAnalyserOptions options = new()
{
    CoverageFormat = CoverageFormat.Auto,
    Directory = ".",
    GlobPatterns = ["**/coverage.xml"]
};

CoverageAnalyser coverageAnalyser = new(options);
Coverage coverage = coverageAnalyser.AnalyseCoverage();

// Analyse only changed lines compared to origin/main
DeltaResult delta = coverageAnalyser.AnalyseDeltaCoverage("origin/main", coverage);
```

By using `CoverageFormat.Auto`, the library will attempt to detect whether each coverage file is in Cobertura or 
SonarQube format. You can also specify a specific format if it is known.

> **Note:** Delta coverage analysis requires Git to be installed and available on the system `PATH`.  
> The `AnalyseDeltaCoverage` method interacts with the underlying Git repository and may throw a
> `GitException` if Git is not installed, not on the `PATH`, the current directory is not a Git
> repository, or if Git commands fail.

## Options

The `CoverageAnalyserOptions` class has the following properties:

- `CoverageFormat`: The format of the coverage file. Options: `Auto`, `SonarQube`, `Cobertura`. Default: `Auto`.
- `Directory`: The directory to search for the coverage file(s) within.
- `GlobPatterns`: The glob patterns to use to search for the coverage file(s). Default: `**/*.xml`.
- `Include`: Optional glob patterns of files to include in the coverage analysis.
- `Exclude`: Optional glob patterns of files to exclude from the coverage analysis.

The `CoverageAnalyser` class can be initialized with either an instance of `CoverageAnalyserOptions` or with an options instance and a `Matcher`.

## Results

The `CoverageAnalyser` class returns a `Coverage` object, which contains all the code coverage metrics. The `Coverage`
object can contain multiple `FileCoverage` objects, which can each contain multiple `LineCoverage` objects.

### DeltaResult Object

- `Coverage` property: A `Coverage` object containing only the filtered changed lines.
- `HasChangedLines` property: Whether any changed lines were found in the coverage report.

### Coverage Object

- `Files` property: A collection of `FileCoverage` objects.
- `CalculateOverallCoverage(coverageType)` method: Calculates the overall code coverage of the specified type for all
  files.
- `CalculatePackageCoverage(packageName, coverageType)` method: Calculates the code coverage of the specified type for
  all files that are part of the specified package.

### FileCoverage Object

- `Lines` property: A collection of `LineCoverage` objects.
- `CalculateFileCoverage(coverageType)` method: Calculates the code coverage of the specified type for the file.
- `CalculateClassCoverage(className, coverageType)` method: Calculates the code coverage of the specified type for all
  lines that are part of the specified class.
- `CalculateMethodCoverage(methodName, coverageType)` method: Calculates the code coverage of the specified type for all
  lines that are part of the specified method.

### LineCoverage Object

- `Line` property: The line number.
- `IsCovered` property: Whether the line is covered.
- `Branches` property: The number of branches in the line.
- `CoveredBranches` property: The number of covered branches in the line.
- `ClassName` property: The name of the class the line is part of.
- `MethodName` property: The name of the method the line is part of.
- `MethodSignature` property: The method signature of the method the line is part of.
- `CalculateLineCoverage(coverageType)` method: Calculates the code coverage of the specified type for the line.