using CommandLine;
using CommandLine.Text;
using CoverageChecker;
using CoverageChecker.CommandLine;
using CoverageChecker.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

Parser parser = new(with => with.HelpWriter = null);
ParserResult<CommandLineOptions> parserResult = parser.ParseArguments<CommandLineOptions>(args);

return parserResult.MapResult(Run, _ => DisplayHelp(parserResult));

static int Run(CommandLineOptions options)
{
    using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddConsole(options => options.FormatterName = "clean")
               .AddConsoleFormatter<ConsoleLogFormatter, ConsoleFormatterOptions>();
        builder.SetMinimumLevel(LogLevel.Information);
        builder.AddFilter("CoverageChecker", LogLevel.Warning);
        builder.AddFilter("CoverageChecker.CommandLine", LogLevel.Information);
    });

    ILogger logger = loggerFactory.CreateLogger("CoverageChecker.CommandLine");
    CoverageAnalyser coverageAnalyser = new(options.CoverageFormat, options.Directory, options.GlobPatterns, loggerFactory);
    Coverage coverage;

    try
    {
        coverage = coverageAnalyser.AnalyseCoverage();
    }
    catch (NoCoverageFilesFoundException)
    {
        logger.LogNoCoverageFilesFound();
        return 1;
    }
    catch (CoverageParseException exception)
    {
        logger.LogErrorParsingCoverageFiles(exception);
        return 1;
    }

    logger.LogParsedCoverage(coverage.Files.Count);

    double lineCoverage = coverage.CalculateOverallCoverage();
    double branchCoverage = coverage.CalculateOverallCoverage(CoverageType.Branch);

    logger.LogLineCoverage(lineCoverage);
    logger.LogBranchCoverage(branchCoverage);

    if (options.LineThreshold > lineCoverage)
    {
        logger.LogLineCoverageBelowThreshold(lineCoverage, options.LineThreshold);
        return 1;
    }

    if (options.BranchThreshold > branchCoverage)
    {
        logger.LogBranchCoverageBelowThreshold(branchCoverage, options.BranchThreshold);
        return 1;
    }

    logger.LogThresholdMet();
    return 0;
}

static int DisplayHelp<T>(ParserResult<T> result)
{
    HelpText? helpText = HelpText.AutoBuild(result, helpText =>
    {
        helpText.AddEnumValuesToHelpText = true;
        return helpText;
    }, e => e);

    Console.WriteLine(helpText);
    return 1;
}