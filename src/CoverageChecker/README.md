# Coverage Checker Library

The Coverage Checker Library is a .NET library for extracting code coverage metrics from various code coverage formats.
It is used by the [Coverage Checker GitHub Action](../CoverageChecker.GitHubAction) and
the [Coverage Checker Command Line Tool](../CoverageChecker.CommandLine).

It can also be used as a development dependency if you need to extract code coverage metrics as part of a .NET project.

For general information about the coverage checker project, see the main [README file](../../README.md).

## Installation

To use the coverage checker library in your .NET project, add
the [CoverageChecker NuGet package](https://www.nuget.org/packages/CoverageChecker) to your project:

```
dotnet add package CoverageChecker
```

## Usage

The `CoverageAnalyser` class is the entry point for extracting code coverage metrics from a coverage file.

The following example shows how to use the `CoverageAnalyser` class to extract code coverage metrics from a Cobertura
coverage file:

```csharp
using CoverageChecker;

CoverageAnalyser coverageAnalyser = new(CoverageFormat.Cobertura, ".", "**/coverage.cobertura.xml");
Coverage coverage = coverageAnalyser.AnalyseCoverage();
```

## Options

The `CoverageAnalyser` class has the following options:

- `coverageFormat`: The format of the coverage file. Options: SonarQube, Cobertura
- `directory`: The directory to search for the coverage file(s) within.

and either one of the following:

- `globPattern`: The glob pattern to use to match the coverage file(s).
- `globPatterns`: The glob patterns to use to match the coverage file(s).
- `matcher`: The glob pattern matcher to use to match the coverage file(s).

## Results

The `CoverageAnalyser` class returns a `Coverage` object, which contains all the code coverage metrics. The `Coverage`
object can contain multiple `FileCoverage` objects, which can each contain multiple `LineCoverage` objects.

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