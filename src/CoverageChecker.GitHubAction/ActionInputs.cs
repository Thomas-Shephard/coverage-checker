using CommandLine;

namespace CoverageChecker.GitHubAction;

public class ActionInputs
{
    [Option('f', "format", Required = true, HelpText = "The format of the coverage files (Supported formats: Cobertura and SonarQube)")]
    public CoverageFormat CoverageFormat { get; set; }

    [Option('g', "glob-patterns", Required = false, HelpText = "The glob pattern to search for coverage files")]
    public IEnumerable<string> GlobPatterns { get; set; } = ["*.xml"];

    [Option('d', "directory", Required = true, HelpText = "The directory where the coverage files are located")]
    public string Directory { get; set; } = Environment.CurrentDirectory;

    [Option('l', "line-threshold", Required = false, HelpText = "The line coverage percentage required")]
    public double LineCoverageThreshold
    {
        get => _lineCoverageThreshold;
        // Divide by 100 to convert the percentage to a decimal
        set
        {
            if (value is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(value), "The value must be between 0 and 100");
            _lineCoverageThreshold = value / 100;
        }
    }

    [Option('b', "branch-threshold", Required = false, HelpText = "The branch coverage percentage required")]
    public double BranchCoverageThreshold
    {
        get => _branchCoverageThreshold;
        // Divide by 100 to convert the percentage to a decimal
        set
        {
            if (value is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(value), "The value must be between 0 and 100");
            _branchCoverageThreshold = value / 100;
        }
    }

    [Option('e', "fail-if-below-threshold", Required = false, Default = true, HelpText = "Fail the action if the coverage is below the required threshold")]
    public bool? FailIfBelowThreshold { get; set; } = true;

    [Option('n', "fail-if-no-files-found", Required = false, Default = true, HelpText = "Fail the action if no coverage files are found")]
    public bool? FailIfNoFilesFound { get; set; } = true;

    private double _branchCoverageThreshold = double.NaN;

    private double _lineCoverageThreshold = double.NaN;
}