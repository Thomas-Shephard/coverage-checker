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

    if (!TryAnalyseCoverage(options, loggerFactory, logger, out Coverage? coverage))
    {
        return 1;
    }

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

static bool TryAnalyseCoverage(CommandLineOptions options, ILoggerFactory loggerFactory, ILogger logger, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Coverage? coverage)
{
    CoverageAnalyser coverageAnalyser = new(options.CoverageFormat, options.Directory, options.GlobPatterns, loggerFactory);

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

static async Task WriteGitHubSummary(Coverage coverage, double lineCoverage, double branchCoverage, CommandLineOptions options, ILogger logger)
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
        summary.AppendLine(FormatMetricRow("Line Coverage", lineCoverage, options.LineThreshold));
        summary.AppendLine(FormatMetricRow("Branch Coverage", branchCoverage, options.BranchThreshold));

        if (lineCoverage < 1.0 || (branchCoverage < 1.0 && !double.IsNaN(branchCoverage)))
        {
            summary.Append(GetFileBreakdown(coverage));
        }

        await File.AppendAllTextAsync(summaryPath, summary.ToString());
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to write GitHub summary to {SummaryPath}", summaryPath);
    }
}

static string FormatMetricRow(string label, double value, double threshold)
{
    bool passed = double.IsNaN(value) || value >= threshold;
    string status = passed ? "✅" : "❌";
    string display = double.IsNaN(value) ? "N/A" : $"{value:P2}";
    return $"| **{label}** | {display} | {threshold:P2} | {status} |";
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
        string lineDisplay = double.IsNaN(item.Line) ? "N/A" : $"{item.Line:P2}";
        string branchDisplay = double.IsNaN(item.Branch) ? "N/A" : $"{item.Branch:P2}";
        string escapedPath = EscapeMarkdown(item.Path);
        sb.AppendLine($"| `{escapedPath}` | {lineDisplay} | {branchDisplay} |");
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