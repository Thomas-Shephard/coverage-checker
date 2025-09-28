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

static void Run(CommandLineOptions options)
{
    CoverageAnalyser coverageAnalyser = new(options.CoverageFormat, options.Directory, options.GlobPatterns);
    Coverage coverage;

    try
    {
        coverage = coverageAnalyser.AnalyseCoverage();
    }
    catch (NoCoverageFilesFoundException)
    {
        ExitWithFailure("No coverage files found.");
        return;
    }
    catch (CoverageParseException exception)
    {
        ExitWithFailure($"Error parsing coverage files.{Environment.NewLine}{exception}");
        return;
    }

    Console.WriteLine($"Parsed coverage information for {coverage.Files.Count} files.");
    Console.WriteLine($"Overall line coverage: {coverage.CalculateOverallCoverage():P2}.");
    Console.WriteLine($"Overall branch coverage: {coverage.CalculateOverallCoverage(CoverageType.Branch):P2}.");

    if (options.LineThreshold > coverage.CalculateOverallCoverage())
    {
        ExitWithFailure($"Line coverage of {coverage.CalculateOverallCoverage():P2} is below the required threshold of {options.LineThreshold:P2}");
        return;
    }

    if (options.BranchThreshold > coverage.CalculateOverallCoverage(CoverageType.Branch))
    {
        ExitWithFailure($"Branch coverage of {coverage.CalculateOverallCoverage(CoverageType.Branch):P2} is below the required threshold of {options.BranchThreshold:P2}");
        return;
    }

    Console.WriteLine("The coverage threshold has been met.");
}

static void DisplayHelp<T>(ParserResult<T> result)
{
    HelpText? helpText = HelpText.AutoBuild(result, helpText =>
    {
        helpText.AddEnumValuesToHelpText = true;
        return helpText;
    }, e => e);

    Console.WriteLine(helpText);
}

static void ExitWithFailure(string errorMessage)
{
    Console.WriteLine(errorMessage);
    Environment.Exit(1);
}