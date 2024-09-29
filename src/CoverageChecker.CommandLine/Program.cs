using CommandLine;
using CommandLine.Text;
using CoverageChecker;
using CoverageChecker.CommandLine;
using CoverageChecker.Results;

Parser parser = new(with => with.HelpWriter = null);
ParserResult<CommandLineOptions> parserResult = parser.ParseArguments<CommandLineOptions>(args);

parserResult.WithParsed(Run)
            .WithNotParsed(_ => DisplayHelp(parserResult));

return;

static void Run(CommandLineOptions options) {
    CoverageAnalyser coverageAnalyser = new(options.CoverageFormat, options.Directory, options.GlobPatterns);
    Coverage coverage;

    try {
        coverage = coverageAnalyser.AnalyseCoverage();
    } catch (NoCoverageFilesFoundException) {
        Console.WriteLine("No coverage files found.");
        return;
    } catch (CoverageParseException exception) {
        Console.WriteLine("Error parsing coverage files.");
        Console.WriteLine(exception);
        return;
    }

    Console.WriteLine($"Successfully parsed coverage information for {coverage.Files.Count} files.");
    Console.WriteLine($"Overall line coverage: {coverage.CalculateOverallCoverage():P2}.");
    Console.WriteLine($"Overall branch coverage: {coverage.CalculateOverallCoverage(CoverageType.Branch):P2}.");
}

static void DisplayHelp<T>(ParserResult<T> result) {
    HelpText? helpText = HelpText.AutoBuild(result, helpText => {
        helpText.AddEnumValuesToHelpText = true;
        return helpText;
    }, e => e);

    Console.WriteLine(helpText);
}