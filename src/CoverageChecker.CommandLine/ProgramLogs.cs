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

    [LoggerMessage(Level = LogLevel.Error, Message = "Line coverage could not be calculated because no applicable lines were found.")]
    public static partial void LogNoApplicableLineCoverage(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Branch coverage of {BranchCoverage:P2} is below the required threshold of {BranchThreshold:P2}")]
    public static partial void LogBranchCoverageBelowThreshold(this ILogger logger, double branchCoverage, double branchThreshold);

    [LoggerMessage(Level = LogLevel.Error, Message = "Delta line coverage of {LineCoverage:P2} is below the required threshold of {LineThreshold:P2}")]
    public static partial void LogDeltaLineCoverageBelowThreshold(this ILogger logger, double lineCoverage, double lineThreshold);

    [LoggerMessage(Level = LogLevel.Error, Message = "Delta line coverage could not be calculated because no applicable changed lines were found.")]
    public static partial void LogNoApplicableDeltaLineCoverage(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Delta branch coverage of {BranchCoverage:P2} is below the required threshold of {BranchThreshold:P2}")]
    public static partial void LogDeltaBranchCoverageBelowThreshold(this ILogger logger, double branchCoverage, double branchThreshold);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Coverage gaps in {FilePath}:")]
    public static partial void LogFileGapHeader(this ILogger logger, string filePath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "  Uncovered lines: {Lines}")]
    public static partial void LogUncoveredLines(this ILogger logger, string lines);

    [LoggerMessage(Level = LogLevel.Warning, Message = "  Partial branches: {Branches}")]
    public static partial void LogPartialBranches(this ILogger logger, string branches);

    [LoggerMessage(Level = LogLevel.Information, Message = "The coverage threshold has been met.")]
    public static partial void LogThresholdMet(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Delta line coverage: {LineCoverage:P2}.")]
    public static partial void LogDeltaLineCoverage(this ILogger logger, double lineCoverage);

    [LoggerMessage(Level = LogLevel.Information, Message = "Delta branch coverage: {BranchCoverage:P2}.")]
    public static partial void LogDeltaBranchCoverage(this ILogger logger, double branchCoverage);

    [LoggerMessage(Level = LogLevel.Information, Message = "No changed lines found in coverage data for delta coverage.")]
    public static partial void LogNoDeltaLinesFound(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Git reported {GitChangedLineCount} changed line(s), but none matched coverage line entries in files present in the report. Check that report paths and line numbers match the checked-out source.")]
    public static partial void LogDeltaLinesMissingFromCoverage(this ILogger logger, int gitChangedLineCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Count} changed file(s) were absent from coverage data: {Files}. These files only fail the check when --strict-delta is enabled.")]
    public static partial void LogDeltaFilesMissingFromCoverage(this ILogger logger, int count, string files);

    [LoggerMessage(Level = LogLevel.Error, Message = "Strict delta coverage failed because {Count} changed file(s) were absent from coverage data: {Files}")]
    public static partial void LogStrictDeltaFilesMissingFromCoverage(this ILogger logger, int count, string files);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to perform delta coverage analysis.")]
    public static partial void LogDeltaAnalysisFailed(this ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Running command: {Command}")]
    public static partial void LogRunningCommand(this ILogger logger, string command);

    [LoggerMessage(Level = LogLevel.Error, Message = "Working directory does not exist: {WorkingDirectory}")]
    public static partial void LogWorkingDirectoryNotFound(this ILogger logger, string workingDirectory);

    [LoggerMessage(Level = LogLevel.Error, Message = "Command failed with exit code {ExitCode}.")]
    public static partial void LogCommandFailed(this ILogger logger, int exitCode);

    [LoggerMessage(Level = LogLevel.Error, Message = "Command timed out after {Timeout} minutes.")]
    public static partial void LogCommandTimedOut(this ILogger logger, int timeout);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Command failed with exit code {ExitCode}. Continuing with coverage analysis as requested.")]
    public static partial void LogCommandFailedWarning(this ILogger logger, int exitCode);

    [LoggerMessage(Level = LogLevel.Critical, Message = "An error occurred while running the command.")]
    public static partial void LogCriticalError(this ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to clean up temporary directory {TempDir}")]
    public static partial void LogCleanupFailed(this ILogger logger, Exception exception, string tempDir);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to write GitHub summary to {SummaryPath}")]
    public static partial void LogGitHubSummaryWriteFailed(this ILogger logger, Exception exception, string summaryPath);
}
