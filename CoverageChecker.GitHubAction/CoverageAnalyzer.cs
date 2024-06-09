using CoverageChecker.Parsers;
using CoverageChecker.Results;

namespace CoverageChecker.GitHubAction;

public class CoverageAnalyzer(ActionInputs options) {
    internal void AnalyzeAsync() {
        Coverage coverage = CreateParser().LoadCoverage();

        CheckLineCoverage(coverage);
        CheckBranchCoverage(coverage);
    }

    private BaseParser CreateParser() {
        if (string.Equals(options.Format, "Cobertura", StringComparison.OrdinalIgnoreCase)) {
            return new CoberturaParser(options.Directory, options.GlobPatterns);
        }

        if (string.Equals(options.Format, "SonarQube", StringComparison.OrdinalIgnoreCase)) {
            return new SonarQubeParser(options.Directory, options.GlobPatterns);
        }

        OutputError("Invalid coverage format", $"Format '{options.Format}' is not supported (Supported formats: Cobertura and SonarQube)");
        Environment.Exit(1);
        return null;
    }

    private void CheckLineCoverage(Coverage coverage) {
        double calculatedLineCoverage = coverage.CalculateOverallCoverage();

        if (double.IsNaN(options.LineCoverageThreshold)) {
            OutputWarning("No line coverage found", "Are there any lines?");
        } else if (double.IsNaN(calculatedLineCoverage)) {
            Console.WriteLine($"Line coverage of {calculatedLineCoverage:P1} but no threshold was provided");
        } else if (calculatedLineCoverage >= options.LineCoverageThreshold) {
            Console.WriteLine($"Line coverage of {calculatedLineCoverage:P1} meets the required threshold of {options.LineCoverageThreshold:P1}");
        } else {
            OutputInsufficientCoverage("Insufficient line coverage", $"Line coverage of {calculatedLineCoverage:P1} is below the required threshold of {options.LineCoverageThreshold:P1}");
        }
    }

    private void CheckBranchCoverage(Coverage coverage) {
        double calculatedBranchCoverage = coverage.CalculateOverallCoverage(CoverageType.Branch);

        if (double.IsNaN(calculatedBranchCoverage)) {
            OutputWarning("No branch coverage found", "Are there any branches?");
        } else if (double.IsNaN(options.BranchCoverageThreshold)) {
            Console.WriteLine($"Branch coverage of {calculatedBranchCoverage:P1} but no threshold was provided");
        } else if (calculatedBranchCoverage >= options.BranchCoverageThreshold) {
            Console.WriteLine($"Branch coverage of {calculatedBranchCoverage:P1} meets the required threshold of {options.BranchCoverageThreshold:P1}");
        } else {
            OutputInsufficientCoverage("Insufficient branch coverage", $"Branch coverage of {calculatedBranchCoverage:P1} is below the required threshold of {options.BranchCoverageThreshold:P1}");
        }
    }

    private void OutputInsufficientCoverage(string title, string message) {
        if (options.FailIfBelowThreshold) {
            OutputWarning(title, message);
            Environment.Exit(1);
        } else {
            OutputError(title, message);
        }
    }

    private static void OutputWarning(string title, string message) {
        Console.WriteLine($"::warning title={title}::{message}");
    }

    private static void OutputError(string title, string message) {
        Console.WriteLine($"::error title={title}::{message}");
    }
}