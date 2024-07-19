using CoverageChecker.Parsers;
using CoverageChecker.Results;
using Microsoft.Extensions.Logging;

namespace CoverageChecker.CommandLineInterface;

public class CoverageAnalyzer(CliArguments options, ILoggerFactory loggerFactory) {
    private readonly ILogger _logger = loggerFactory.CreateLogger<CoverageAnalyzer>();

    internal Task AnalyzeAsync() {
        _logger.LogInformation("Analyzing coverage");

        BaseParser parser = options.Format switch {
            CoverageFormat.Cobertura => new CoberturaParser(options.Directory, options.GlobPatterns, options.FailIfNoFilesFound),
            CoverageFormat.SonarQube => new SonarQubeParser(options.Directory, options.GlobPatterns, options.FailIfNoFilesFound),
            _ => throw new InvalidOperationException($"Format '{options.Format}' is not supported (Supported formats: Cobertura and SonarQube)")
        };

        Coverage coverage = parser.LoadCoverage();

        Console.WriteLine($"Coverage: {coverage}");

        return Task.CompletedTask;
    }
}