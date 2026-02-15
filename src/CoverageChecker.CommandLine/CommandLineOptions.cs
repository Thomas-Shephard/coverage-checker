using CommandLine;

namespace CoverageChecker.CommandLine;

public class CommandLineOptions
{
    [Option('f', "format", Required = false, HelpText = "Format of coverage files. Default: Auto", Default = CoverageFormat.Auto)]
    public CoverageFormat CoverageFormat { get; init; } = CoverageFormat.Auto;

    [Option('d', "directory", Required = false, HelpText = "Directory where coverage files are located. Default: Current directory")]
    public string Directory { get; init; } = Environment.CurrentDirectory;

    [Option('g', "glob-patterns", Required = false, HelpText = "Glob patterns of coverage file locations. Default: **/*.xml", Default = new[] { "**/*.xml" })]
    public IEnumerable<string> GlobPatterns { get; init; } = ["**/*.xml"];

    [Option('i', "include", Required = false, HelpText = "Glob patterns of files to include in the coverage analysis.")]
    public IEnumerable<string>? Include { get; init; }

    [Option('e', "exclude", Required = false, HelpText = "Glob patterns of files to exclude from the coverage analysis.")]
    public IEnumerable<string>? Exclude { get; init; }

    private readonly double _lineThreshold = 0.8;
    [Option('l', "line-threshold", Required = false, HelpText = "Line coverage threshold (percentage). Default: 80", Default = 80.0)]
    public double LineThreshold
    {
        get => _lineThreshold;
        init => _lineThreshold = ValidateThreshold(value, nameof(LineThreshold)) / 100;
    }

    private readonly double _branchThreshold = 0.8;

    [Option('b', "branch-threshold", Required = false, HelpText = "Branch coverage threshold (percentage). Default: 80", Default = 80.0)]
    public double BranchThreshold
    {
        get => _branchThreshold;
        init => _branchThreshold = ValidateThreshold(value, nameof(BranchThreshold)) / 100;
    }

    [Option("delta", Required = false, HelpText = "Calculate coverage for changed lines only.")]
    public bool Delta { get; init; }

    [Option("delta-base", Required = false, HelpText = "Base branch or commit to compare against for delta coverage. Default: origin/main", Default = "origin/main")]
    public string DeltaBase { get; init; } = "origin/main";

    private readonly double _renameThreshold = 0.5;

    [Option('r', "rename-threshold", Required = false, HelpText = "The similarity threshold for rename detection (percentage). Default: 50", Default = 50.0)]
    public double RenameThreshold
    {
        get => _renameThreshold;
        init => _renameThreshold = ValidateThreshold(value, nameof(RenameThreshold)) / 100;
    }

    private static double ValidateThreshold(double value, string paramName)
    {
        if (double.IsNaN(value) || value is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(paramName, $"{paramName} must be between 0 and 100");
        }

        return value;
    }
}
