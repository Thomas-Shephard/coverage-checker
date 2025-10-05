
using CommandLine;
using CommandLine.Text;
using CoverageChecker.Results;

namespace CoverageChecker.CommandLine;

internal class App(TextWriter outputWriter, TextWriter errorWriter)
{
    internal int Run(string[] args)
    {
        Parser parser = new(with => with.HelpWriter = null);
        ParserResult<CommandLineOptions> parserResult = parser.ParseArguments<CommandLineOptions>(args);

        return parserResult.MapResult(
            RunAndReturnExitCode,
            _ => DisplayHelp(parserResult));
    }

    private int RunAndReturnExitCode(CommandLineOptions options)
    {
        CoverageAnalyser coverageAnalyser = new(options.CoverageFormat, options.Directory, options.GlobPatterns);
        Coverage coverage;

        try
        {
            coverage = coverageAnalyser.AnalyseCoverage();
        }
        catch (NoCoverageFilesFoundException)
        {
            return ExitWithFailure("No coverage files found.");
        }
        catch (CoverageParseException exception)
        {
            return ExitWithFailure($"Error parsing coverage files.{Environment.NewLine}{exception.Message}");
        }

        outputWriter.WriteLine($"Parsed coverage information for {coverage.Files.Count} files.");
        outputWriter.WriteLine($"Overall line coverage: {coverage.CalculateOverallCoverage():P2}.");
        outputWriter.WriteLine($"Overall branch coverage: {coverage.CalculateOverallCoverage(CoverageType.Branch):P2}.");

        if (options.LineThreshold > coverage.CalculateOverallCoverage())
        {
            return ExitWithFailure(
                $"Line coverage of {coverage.CalculateOverallCoverage():P2} is below the required threshold of {options.LineThreshold:P2}");
        }

        if (options.BranchThreshold > coverage.CalculateOverallCoverage(CoverageType.Branch))
        {
            return ExitWithFailure(
                $"Branch coverage of {coverage.CalculateOverallCoverage(CoverageType.Branch):P2} is below the required threshold of {options.BranchThreshold:P2}");
        }

        outputWriter.WriteLine("The coverage threshold has been met.");
        return 0;
    }

    private int DisplayHelp<T>(ParserResult<T> result)
    {
        HelpText? helpText = HelpText.AutoBuild(result, helpText =>
        {
            helpText.AddEnumValuesToHelpText = true;
            return helpText;
        }, e => e);

        outputWriter.WriteLine(helpText);
        return 1;
    }

    private int ExitWithFailure(string errorMessage)
    {
        errorWriter.WriteLine(errorMessage);
        return 1;
    }
}
