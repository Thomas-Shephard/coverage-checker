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

    [Option('l', "line-threshold", Required = false, HelpText = "Line coverage threshold. Default: 80")]
    public double LineThreshold { get; init; } = 80;

    [Option('b', "branch-threshold", Required = false, HelpText = "Branch coverage threshold. Default: 80")]
    public double BranchThreshold { get; init; } = 80;
}