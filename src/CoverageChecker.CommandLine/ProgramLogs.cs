using Microsoft.Extensions.Logging;

namespace CoverageChecker.CommandLine;

internal static partial class ProgramLogs
{
    [LoggerMessage(Level = LogLevel.Error, Message = "No coverage files found.")]
    public static partial void LogNoCoverageFilesFound(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error parsing coverage files.")]
    public static partial void LogErrorParsingCoverageFiles(this ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Parsed coverage information for {Count} files.")]
    public static partial void LogParsedCoverage(this ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Overall line coverage: {LineCoverage:P2}.")]
    public static partial void LogLineCoverage(this ILogger logger, double lineCoverage);

    [LoggerMessage(Level = LogLevel.Information, Message = "Overall branch coverage: {BranchCoverage:P2}.")]
    public static partial void LogBranchCoverage(this ILogger logger, double branchCoverage);

    [LoggerMessage(Level = LogLevel.Error, Message = "Line coverage of {LineCoverage:P2} is below the required threshold of {LineThreshold:P2}")]
    public static partial void LogLineCoverageBelowThreshold(this ILogger logger, double lineCoverage, double lineThreshold);

    [LoggerMessage(Level = LogLevel.Error, Message = "Branch coverage of {BranchCoverage:P2} is below the required threshold of {BranchThreshold:P2}")]
    public static partial void LogBranchCoverageBelowThreshold(this ILogger logger, double branchCoverage, double branchThreshold);

    [LoggerMessage(Level = LogLevel.Error, Message = "Delta line coverage of {LineCoverage:P2} is below the required threshold of {LineThreshold:P2}")]
    public static partial void LogDeltaLineCoverageBelowThreshold(this ILogger logger, double lineCoverage, double lineThreshold);

    [LoggerMessage(Level = LogLevel.Error, Message = "Delta branch coverage of {BranchCoverage:P2} is below the required threshold of {BranchThreshold:P2}")]
    public static partial void LogDeltaBranchCoverageBelowThreshold(this ILogger logger, double branchCoverage, double branchThreshold);

    [LoggerMessage(Level = LogLevel.Information, Message = "The coverage threshold has been met.")]
    public static partial void LogThresholdMet(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Delta line coverage: {LineCoverage:P2}.")]
    public static partial void LogDeltaLineCoverage(this ILogger logger, double lineCoverage);

    [LoggerMessage(Level = LogLevel.Information, Message = "Delta branch coverage: {BranchCoverage:P2}.")]
    public static partial void LogDeltaBranchCoverage(this ILogger logger, double branchCoverage);

    [LoggerMessage(Level = LogLevel.Information, Message = "No changed lines found for delta coverage.")]
    public static partial void LogNoDeltaLinesFound(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to perform delta coverage analysis.")]
    public static partial void LogDeltaAnalysisFailed(this ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to write GitHub summary to {SummaryPath}")]
    public static partial void LogGitHubSummaryWriteFailed(this ILogger logger, Exception exception, string summaryPath);
}
