using System.Globalization;
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

    using ILoggerFactory loggerFactory = CreateLoggerFactory(isGitHubActions);
    ILogger logger = loggerFactory.CreateLogger("CoverageChecker.CommandLine");

    CoverageAnalyser coverageAnalyser = new(options.CoverageFormat, options.Directory, options.GlobPatterns, loggerFactory);

    if (!TryAnalyseCoverage(coverageAnalyser, logger, out Coverage? coverage))
    {
        return 1;
    }

    CoverageResult result = new(
        coverage,
        coverage.CalculateOverallCoverage(),
        coverage.CalculateOverallCoverage(CoverageType.Branch),
        null,
        double.NaN,
        double.NaN,
        false
    );

    logger.LogLineCoverage(result.LineCoverage);
    logger.LogBranchCoverage(result.BranchCoverage);

    if (options.Delta)
    {
        if (!TryAnalyseDeltaCoverage(coverageAnalyser, coverage, options, logger, out Coverage? deltaCoverage, out bool hasDeltaChangedLines))
        {
            return 1;
        }

        if (hasDeltaChangedLines && deltaCoverage != null)
        {
            result = result with
            {
                DeltaCoverage = deltaCoverage,
                HasDeltaChangedLines = true,
                DeltaLineCoverage = deltaCoverage.CalculateOverallCoverage(),
                DeltaBranchCoverage = deltaCoverage.CalculateOverallCoverage(CoverageType.Branch)
            };

            logger.LogDeltaLineCoverage(result.DeltaLineCoverage);
            logger.LogDeltaBranchCoverage(result.DeltaBranchCoverage);
        }
        else
        {
            logger.LogNoDeltaLinesFound();
        }
    }

    if (isGitHubActions)
    {
        await WriteGitHubSummary(result, options, logger);
    }

    return CheckThresholds(result, options, logger);
}

static bool TryAnalyseDeltaCoverage(CoverageAnalyser analyser, Coverage coverage, CommandLineOptions options, ILogger logger, out Coverage? deltaCoverage, out bool hasChangedLines)
{
    deltaCoverage = null;
    hasChangedLines = false;
    try
    {
        DeltaResult deltaResult = analyser.AnalyseDeltaCoverage(options.DeltaBase, coverage);
        deltaCoverage = deltaResult.Coverage;
        hasChangedLines = deltaResult.HasChangedLines;
        return true;
    }
    catch (Exception ex) when (ex is GitException or ArgumentException)
    {
        logger.LogDeltaAnalysisFailed(ex);
        return false;
    }
}

static int CheckThresholds(CoverageResult result, CommandLineOptions options, ILogger logger)
{
    if (options.LineThreshold > result.LineCoverage)
    {
        logger.LogLineCoverageBelowThreshold(result.LineCoverage, options.LineThreshold);
        return 1;
    }

    if (options.BranchThreshold > result.BranchCoverage)
    {
        logger.LogBranchCoverageBelowThreshold(result.BranchCoverage, options.BranchThreshold);
        return 1;
    }

    if (options.Delta && result is { HasDeltaChangedLines: true, DeltaCoverage: not null })
    {
        if (!double.IsNaN(result.DeltaLineCoverage) && options.LineThreshold > result.DeltaLineCoverage)
        {
            logger.LogDeltaLineCoverageBelowThreshold(result.DeltaLineCoverage, options.LineThreshold);
            return 1;
        }

        if (!double.IsNaN(result.DeltaBranchCoverage) && options.BranchThreshold > result.DeltaBranchCoverage)
        {
            logger.LogDeltaBranchCoverageBelowThreshold(result.DeltaBranchCoverage, options.BranchThreshold);
            return 1;
        }
    }

    logger.LogThresholdMet();
    return 0;
}

static ILoggerFactory CreateLoggerFactory(bool isGitHubActions)
{
    return LoggerFactory.Create(builder =>
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
            builder.AddFilter("CoverageChecker", LogLevel.Warning);
        }

        builder.SetMinimumLevel(LogLevel.Information);
        builder.AddFilter("CoverageChecker.CommandLine", LogLevel.Information);
    });
}

static bool TryAnalyseCoverage(CoverageAnalyser coverageAnalyser, ILogger logger, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Coverage? coverage)
{
    try
    {
        coverage = coverageAnalyser.AnalyseCoverage();
        logger.LogParsedCoverage(coverage.Files.Count);
        return true;
    }
    catch (NoCoverageFilesFoundException)
    {
        logger.LogNoCoverageFilesFound();
    }
    catch (CoverageParseException exception)
    {
        logger.LogErrorParsingCoverageFiles(exception);
    }

    coverage = null;
    return false;
}

static async Task WriteGitHubSummary(CoverageResult result, CommandLineOptions options, ILogger logger)
{
    string? summaryPath = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
    if (string.IsNullOrEmpty(summaryPath)) return;

    try
    {
        StringBuilder summary = new();
        summary.AppendLine("### Coverage Report Summary");
        summary.AppendLine();
        summary.AppendLine("| Metric | Current | Threshold | Status |");
        summary.AppendLine("| :--- | :---: | :---: | :---: |");
        summary.AppendLine(FormatMetricRow("Line Coverage", result.LineCoverage, options.LineThreshold));
        summary.AppendLine(FormatMetricRow("Branch Coverage", result.BranchCoverage, options.BranchThreshold));

        if (options.Delta)
        {
            if (result.HasDeltaChangedLines)
            {
                summary.AppendLine(FormatMetricRow("Delta Line Coverage", result.DeltaLineCoverage, options.LineThreshold));
                summary.AppendLine(FormatMetricRow("Delta Branch Coverage", result.DeltaBranchCoverage, options.BranchThreshold));
            }
            else
            {
                summary.AppendLine("| Delta Coverage | N/A (No changed lines) | - | ✅ |");
            }
        }

        if (ShouldShowBreakdown(result.LineCoverage, result.BranchCoverage))
        {
            summary.Append(GetFileBreakdown(result.OverallCoverage));
        }

        if (result is { DeltaCoverage: not null, HasDeltaChangedLines: true } && ShouldShowBreakdown(result.DeltaLineCoverage, result.DeltaBranchCoverage))
        {
            summary.AppendLine();
            summary.AppendLine("#### Delta File Breakdown");
            summary.Append(GetFileBreakdown(result.DeltaCoverage));
        }

        await File.AppendAllTextAsync(summaryPath, summary.ToString());
    }
    catch (Exception ex)
    {
        logger.LogGitHubSummaryWriteFailed(ex, summaryPath);
    }
}

static bool ShouldShowBreakdown(double lineCoverage, double branchCoverage)
{
    return lineCoverage < 1.0 || (branchCoverage < 1.0 && !double.IsNaN(branchCoverage));
}

static string FormatMetricRow(string label, double value, double threshold)
{
    bool passed = double.IsNaN(value) || value >= threshold;
    string status = passed ? "✅" : "❌";
    string display = double.IsNaN(value) ? "N/A" : value.ToString("P2", CultureInfo.InvariantCulture);
    return $"| **{label}** | {display} | {threshold.ToString("P2", CultureInfo.InvariantCulture)} | {status} |";
}

static string GetFileBreakdown(Coverage coverage)
{
    StringBuilder sb = new();
    sb.AppendLine();
    sb.AppendLine("#### File Breakdown (Top 10 lowest)");
    sb.AppendLine("| File | Line Coverage | Branch Coverage |");
    sb.AppendLine("| :--- | :---: | :---: |");

    var lowestFiles = coverage.Files
                              .Select(f => new
                              {
                                  f.Path,
                                  Line = f.CalculateFileCoverage(),
                                  Branch = f.CalculateFileCoverage(CoverageType.Branch)
                              })
                              .Where(f => !double.IsNaN(f.Line) && (f.Line < 1.0 || (f.Branch < 1.0 && !double.IsNaN(f.Branch))))
                              .OrderBy(f => double.IsNaN(f.Branch) ? f.Line : Math.Min(f.Line, f.Branch))
                              .Take(10);

    foreach (var item in lowestFiles)
    {
        string lineDisplay = double.IsNaN(item.Line) ? "N/A" : item.Line.ToString("P2", CultureInfo.InvariantCulture);
        string branchDisplay = double.IsNaN(item.Branch) ? "N/A" : item.Branch.ToString("P2", CultureInfo.InvariantCulture);
        string escapedPath = EscapeMarkdown(item.Path);
        sb.AppendLine(CultureInfo.InvariantCulture, $"| `{escapedPath}` | {lineDisplay} | {branchDisplay} |");
    }

    return sb.ToString();
}

static string EscapeMarkdown(string text)
{
    return text.Replace("|", "\\|").Replace("`", "\\` ");
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
