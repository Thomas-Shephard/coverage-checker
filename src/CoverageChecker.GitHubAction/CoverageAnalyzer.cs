using CoverageChecker.Results;

namespace CoverageChecker.GitHubAction;

public class CoverageAnalyzer(ActionInputs options)
{
    internal void AnalyzeAsync()
    {
        CoverageAnalyser coverageAnalyser = new(options.CoverageFormat, options.Directory, options.GlobPatterns);
        Coverage coverage;

        try
        {
            coverage = coverageAnalyser.AnalyseCoverage();
        }
        catch (NoCoverageFilesFoundException)
        {
            if (options.FailIfNoFilesFound.GetValueOrDefault(true))
            {
                OutputError("Coverage files not found", "No coverage files found.");
                Environment.Exit(1);
            }
            else
            {
                OutputWarning("Coverage files not found", "No coverage files found.");
            }

            return;
        }


        CheckLineCoverage(coverage, out double calculatedLineCoverage);
        CheckBranchCoverage(coverage, out double calculatedBranchCoverage);

        string? githubOutput = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");

        if (githubOutput is not null)
        {
            using StreamWriter writer = new(githubOutput, true);
            writer.WriteLine($"line-coverage={calculatedLineCoverage * 100:F2}");
            writer.WriteLine($"branch-coverage={calculatedBranchCoverage * 100:F2}");
        }
    }

    private void CheckLineCoverage(Coverage coverage, out double calculatedLineCoverage)
    {
        calculatedLineCoverage = coverage.CalculateOverallCoverage();

        if (double.IsNaN(options.LineCoverageThreshold))
        {
            OutputWarning("No line coverage found", "Are there any lines?");
        }
        else if (double.IsNaN(calculatedLineCoverage))
        {
            Console.WriteLine($"Line coverage of {calculatedLineCoverage:P1} but no threshold was provided");
        }
        else if (calculatedLineCoverage >= options.LineCoverageThreshold)
        {
            Console.WriteLine($"Line coverage of {calculatedLineCoverage:P1} meets the required threshold of {options.LineCoverageThreshold:P1}");
        }
        else
        {
            OutputInsufficientCoverage("Insufficient line coverage", $"Line coverage of {calculatedLineCoverage:P1} is below the required threshold of {options.LineCoverageThreshold:P1}");
        }
    }

    private void CheckBranchCoverage(Coverage coverage, out double calculatedBranchCoverage)
    {
        calculatedBranchCoverage = coverage.CalculateOverallCoverage(CoverageType.Branch);

        if (double.IsNaN(calculatedBranchCoverage))
        {
            OutputWarning("No branch coverage found", "Are there any branches?");
        }
        else if (double.IsNaN(options.BranchCoverageThreshold))
        {
            Console.WriteLine($"Branch coverage of {calculatedBranchCoverage:P1} but no threshold was provided");
        }
        else if (calculatedBranchCoverage >= options.BranchCoverageThreshold)
        {
            Console.WriteLine($"Branch coverage of {calculatedBranchCoverage:P1} meets the required threshold of {options.BranchCoverageThreshold:P1}");
        }
        else
        {
            OutputInsufficientCoverage("Insufficient branch coverage", $"Branch coverage of {calculatedBranchCoverage:P1} is below the required threshold of {options.BranchCoverageThreshold:P1}");
        }
    }

    private void OutputInsufficientCoverage(string title, string message)
    {
        if (options.FailIfBelowThreshold.GetValueOrDefault(true))
        {
            OutputError(title, message);
            Environment.Exit(1);
        }
        else
        {
            OutputWarning(title, message);
        }
    }

    private static void OutputWarning(string title, string message)
    {
        Console.WriteLine($"::warning title={title}::{message}");
    }

    private static void OutputError(string title, string message)
    {
        Console.WriteLine($"::error title={title}::{message}");
    }
}