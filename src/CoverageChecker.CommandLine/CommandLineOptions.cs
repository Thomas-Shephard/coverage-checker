using CommandLine;

namespace CoverageChecker.CommandLine;

/// <summary>
/// Represents the command line options for the coverage checker.
/// </summary>
public class CommandLineOptions
{
    /// <summary>
    /// Gets or sets the format of coverage files.
    /// </summary>
    [Option('f', "format", Required = false, HelpText = "Format of coverage files. Default: Auto", Default = CoverageFormat.Auto)]
    public CoverageFormat CoverageFormat { get; init; } = CoverageFormat.Auto;

    /// <summary>
    /// Gets or sets the directory where coverage files are located.
    /// </summary>
    [Option('d', "directory", Required = false, HelpText = "Directory where coverage files are located. Default: Current directory")]
    public string Directory { get; init; } = Environment.CurrentDirectory;

    /// <summary>
    /// Gets or sets the glob patterns of coverage file locations.
    /// </summary>
    [Option('g', "glob-patterns", Required = false, HelpText = "Glob patterns of coverage file locations. Default: **/*.xml", Default = new[] { "**/*.xml" })]
    public IEnumerable<string> GlobPatterns { get; init; } = ["**/*.xml"];

    /// <summary>
    /// Gets or sets the glob patterns of files to include in the coverage analysis.
    /// </summary>
    [Option('i', "include", Required = false, HelpText = "Glob patterns of files to include in the coverage analysis.")]
    public IEnumerable<string>? Include { get; init; }

    /// <summary>
    /// Gets or sets the glob patterns of files to exclude from the coverage analysis.
    /// </summary>
    [Option('e', "exclude", Required = false, HelpText = "Glob patterns of files to exclude from the coverage analysis.")]
    public IEnumerable<string>? Exclude { get; init; }

    private readonly double _lineThreshold = 0.8;

    /// <summary>
    /// Gets or sets the line coverage threshold. The setter expects a percentage (0-100), which is stored as a decimal (0.0-1.0).
    /// </summary>
    [Option('l', "line-threshold", Required = false, HelpText = "Line coverage threshold (percentage). Default: 80", Default = 80d)]
    public double LineThreshold
    {
        get => _lineThreshold;
        init => _lineThreshold = ValidateThreshold(value, nameof(LineThreshold)) / 100;
    }

    private readonly double _branchThreshold = 0.8;

    /// <summary>
    /// Gets or sets the branch coverage threshold. The setter expects a percentage (0-100), which is stored as a decimal (0.0-1.0).
    /// </summary>
    [Option('b', "branch-threshold", Required = false, HelpText = "Branch coverage threshold (percentage). Default: 80", Default = 80d)]
    public double BranchThreshold
    {
        get => _branchThreshold;
        init => _branchThreshold = ValidateThreshold(value, nameof(BranchThreshold)) / 100;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to calculate coverage for changed lines only.
    /// </summary>
    [Option("delta", Required = false, HelpText = "Calculate coverage for changed lines only.")]
    public bool Delta { get; init; }

    /// <summary>
    /// Gets or sets the base branch or commit to compare against for delta coverage.
    /// </summary>
    [Option("delta-base", Required = false, HelpText = "Base branch or commit to compare against for delta coverage. Default: origin/main", Default = "origin/main")]
    public string DeltaBase { get; init; } = "origin/main";
    
    /// <summary>
    /// Gets or sets the path to a .runsettings file to use for include/exclude patterns.
    /// </summary>
    [Option("runsettings", Required = false, HelpText = "Path to a .runsettings file to use for include/exclude patterns. Default: .runsettings", Default = ".runsettings")]
    public string RunSettings { get; init; } = ".runsettings";

    private readonly double _renameThreshold = 0.5;

    /// <summary>
    /// Gets or sets the similarity threshold for rename detection. The setter expects a percentage (0-100), which is stored as a decimal (0.0-1.0).
    /// </summary>
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
