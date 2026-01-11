using System.Text;
using CommandLine;
using CommandLine.Text;
using CoverageChecker;
using CoverageChecker.CommandLine;
using CoverageChecker.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

Parser parser = new(with => with.HelpWriter = null);
ParserResult<CommandLineOptions> parserResult = parser.ParseArguments<CommandLineOptions>(args);

return await parserResult.MapResult(Run, _ => Task.FromResult(DisplayHelp(parserResult)));

static async Task<int> Run(CommandLineOptions options)
{
    bool isGitHubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";

    using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
    {
        if (isGitHubActions)
        {
            builder.AddConsole(opt => opt.FormatterName = "github")
                   .AddConsoleFormatter<GitHubWorkflowFormatter, ConsoleFormatterOptions>();
            builder.AddFilter("CoverageChecker", LogLevel.Information);
        }
        else
        {
            builder.AddConsole(opt => opt.FormatterName = "clean")
                   .AddConsoleFormatter<ConsoleLogFormatter, ConsoleFormatterOptions>();
        }

        if (!isGitHubActions)
        {
            builder.AddFilter("CoverageChecker", LogLevel.Warning);
        }

        builder.SetMinimumLevel(LogLevel.Information);
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

    if (isGitHubActions)
    {
        await WriteGitHubSummary(coverage, lineCoverage, branchCoverage, options, logger);
    }

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

static async Task WriteGitHubSummary(Coverage coverage, double lineCoverage, double branchCoverage, CommandLineOptions options, ILogger logger)
{
    string? summaryPath = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
    if (string.IsNullOrEmpty(summaryPath)) return;

    try
    {
        bool linePassed = double.IsNaN(lineCoverage) || lineCoverage >= options.LineThreshold;
        bool branchPassed = double.IsNaN(branchCoverage) || branchCoverage >= options.BranchThreshold;

        string lineStatus = linePassed ? "✅" : "❌";
        string branchStatus = branchPassed ? "✅" : "❌";
        string lineDisplay = double.IsNaN(lineCoverage) ? "N/A" : $"{lineCoverage:P2}";
        string branchDisplay = double.IsNaN(branchCoverage) ? "N/A" : $"{branchCoverage:P2}";

        StringBuilder summary = new();
        summary.AppendLine("### Coverage Report Summary");
        summary.AppendLine();
        summary.AppendLine("| Metric | Current | Threshold | Status |");
        summary.AppendLine("| :--- | :---: | :---: | :---: |");
        summary.AppendLine($"| **Line Coverage** | {lineDisplay} | {options.LineThreshold:P2} | {lineStatus} |");
        summary.AppendLine($"| **Branch Coverage** | {branchDisplay} | {options.BranchThreshold:P2} | {branchStatus} |");

        if (lineCoverage < 1.0 || (branchCoverage < 1.0 && !double.IsNaN(branchCoverage)))
        {
            summary.AppendLine();
            summary.AppendLine("#### File Breakdown (Top 10 lowest)");
            summary.AppendLine("| File | Line Coverage | Branch Coverage |");
            summary.AppendLine("| :--- | :---: | :---: |");

            var lowestCoverageFiles = coverage.Files
                                              .Select(f => new
                                              {
                                                  File = f,
                                                  Line = f.CalculateFileCoverage(),
                                                  Branch = f.CalculateFileCoverage(CoverageType.Branch)
                                              })
                                              .Where(f => !double.IsNaN(f.Line))
                                              .OrderBy(f => double.IsNaN(f.Branch) ? f.Line : Math.Min(f.Line, f.Branch))
                                              .Take(10);

            foreach (var item in lowestCoverageFiles)
            {
                string fileBranchDisplay = double.IsNaN(item.Branch) ? "N/A" : $"{item.Branch:P2}";
                string fileLineDisplay = double.IsNaN(item.Line) ? "N/A" : $"{item.Line:P2}";
                summary.AppendLine($"| `{item.File.Path}` | {fileLineDisplay} | {fileBranchDisplay} |");
            }
        }

        await File.AppendAllTextAsync(summaryPath, summary.ToString());
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to write GitHub summary to {SummaryPath}", summaryPath);
    }
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