using CoverageChecker.Parsers;
using CoverageChecker.Results;
using Microsoft.Extensions.Logging;

namespace CoverageChecker.CommandLineInterface;

public class CoverageAnalyzer(CliArguments options, ILoggerFactory loggerFactory) {
    private readonly ILogger _logger = loggerFactory.CreateLogger<CoverageAnalyzer>();

    internal async Task AnalyzeAsync() {
        _logger.LogInformation("Analyzing coverage");

        CoverageAnalyser analyser = new(options.Format, options.Directory, options.GlobPatterns.ToArray(), options.FailIfNoFilesFound);

        await analyser.AnalyseCoverage();
    }
}