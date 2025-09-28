using CommandLine;

namespace CoverageChecker.CommandLine;

public class CommandLineOptions
{
    [Option('f', "format", Required = true, HelpText = "Format of coverage files.")]
    public CoverageFormat CoverageFormat { get; init; }

    [Option('d', "directory", Required = false, HelpText = "Directory where coverage files are located. Default: Current directory")]
    public string Directory { get; init; } = Environment.CurrentDirectory;

    [Option('g', "glob-patterns", Required = false, HelpText = "Glob patterns of coverage file locations. Default: *.xml", Default = new[] { "*.xml" })]
    public IEnumerable<string> GlobPatterns { get; init; } = ["*.xml"];

    private readonly double _lineThreshold = 0.8;
    [Option('l', "line-threshold", Required = false, HelpText = "Line coverage threshold (percentage). Default: 80")]
    public double LineThreshold
    {
        get => _lineThreshold;
        init
        {
            if (value is < 0 or > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(LineThreshold), "Line threshold must be between 0 and 100");
            }

            _lineThreshold = value / 100;
        }
    }

    private readonly double _branchThreshold = 0.8;

    [Option('b', "branch-threshold", Required = false, HelpText = "Branch coverage threshold (percentage). Default: 80")]
    public double BranchThreshold
    {
        get => _branchThreshold;
        init
        {
            if (value is < 0 or > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(BranchThreshold), "Branch threshold must be between 0 and 100");
            }

            _branchThreshold = value / 100;
        }
    }
}