using CommandLine;

namespace CoverageChecker.CommandLine;

public class CommandLineOptions {
    [Option('f', "format", Required = true, HelpText = "Format of coverage files.")]
    public CoverageFormat CoverageFormat { get; init; }

    [Option('d', "directory", Required = false, HelpText = "Directory where coverage files are located. Default: Current directory")]
    public string Directory { get; init; } = Environment.CurrentDirectory;

    [Option('g', "glob-patterns", Required = false, HelpText = "Glob patterns of coverage file locations. Default: *.xml")]
    public IEnumerable<string> GlobPatterns { get; init; } = ["*.xml"];
}