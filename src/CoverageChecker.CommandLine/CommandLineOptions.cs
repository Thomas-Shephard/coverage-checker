using CommandLine;

namespace CoverageChecker.CommandLine;

/// <summary>
/// Base record for command line options.
/// </summary>
public abstract record CommandLineOptions
{
    /// <summary>
    /// Gets or sets the format of coverage files.
    /// </summary>
    [Option('f', "format", Required = false, HelpText = "Format of coverage files. Default: Auto", Default = CoverageFormat.Auto)]
    public CoverageFormat CoverageFormat { get; init; } = CoverageFormat.Auto;

    /// <summary>
    /// Gets or sets the directory where coverage files are located.
    /// </summary>
    public virtual string Directory { get; init; } = Environment.CurrentDirectory;

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
    /// Gets or sets a value indicating whether changed files missing from coverage data should fail delta coverage.
    /// </summary>
    [Option("strict-delta", Required = false, HelpText = "Fail delta coverage when Git changed files with changed lines are absent from coverage data.")]
    public bool StrictDelta { get; init; }

    /// <summary>
    /// Gets or sets the base branch or commit to compare against for delta coverage.
    /// </summary>
    [Option("delta-base", Required = false, HelpText = "Base branch or commit to compare against for delta coverage. Default: origin/main", Default = "origin/main")]
    public string DeltaBase { get; init; } = "origin/main";

    private readonly double _renameThreshold = 0.5;

    /// <summary>
    /// Gets or sets the similarity threshold for rename detection. The setter expects a percentage (0-100), which is stored as a decimal (0.0-1.0).
    /// </summary>
    [Option("rename-threshold", Required = false, HelpText = "The similarity threshold for rename detection (percentage). Default: 50", Default = 50.0)]
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

/// <summary>
/// Represents the command line options for the check command.
/// </summary>
[Verb("check", isDefault: true, HelpText = "Check coverage of existing files.")]
public record CheckOptions : CommandLineOptions
{
    /// <summary>
    /// Gets or sets the directory where coverage files are located.
    /// </summary>
    [Option('d', "directory", Required = false, HelpText = "Directory where coverage files are located. Default: Current directory")]
    public override string Directory { get; init; } = Environment.CurrentDirectory;
}

/// <summary>
/// Represents the command line options for the run command.
/// </summary>
[Verb("run", HelpText = "Run a command and check the resulting coverage.")]
public record RunOptions : CommandLineOptions
{
    /// <summary>
    /// Gets or sets the command to run.
    /// </summary>
    [Option('c', "command", Required = true, HelpText = "The command to run.")]
    public string Command { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the directory where coverage results will be stored. If not specified, a temporary directory will be used.
    /// </summary>
    [Option('o', "output", Required = false, HelpText = "The directory where coverage results will be stored. If not specified, a temporary directory will be used.")]
    public string? Output { get; init; }

    /// <summary>
    /// Gets or sets the working directory where the command will be executed. If not specified, the current directory will be used.
    /// </summary>
    [Option("working-directory", Required = false, HelpText = "The working directory where the command will be executed. Default: Current directory")]
    public string? WorkingDirectory { get; init; }

    private readonly int _timeout = 30;

    /// <summary>
    /// Gets or sets the maximum amount of time, in minutes, that the specified command is allowed to run before being automatically terminated.
    /// </summary>
    [Option('t', "timeout", Required = false, HelpText = "The timeout for the command in minutes. Default: 30", Default = 30)]
    public int Timeout
    {
        get => _timeout;
        init => _timeout = value is < 1 and not -1 ? throw new ArgumentOutOfRangeException(nameof(Timeout), "Timeout must be at least 1 minute, or -1 for infinite.") : value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to continue with coverage analysis even if the command fails.
    /// </summary>
    [Option("continue-on-failure", Required = false, HelpText = "Continue with coverage analysis even if the command fails.")]
    public bool ContinueOnFailure { get; init; }
}
