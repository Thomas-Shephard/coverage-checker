using CoverageChecker.Results;
using CoverageChecker.Services;
using Microsoft.Extensions.Logging;

namespace CoverageChecker.Parsers;

internal class ParserFactory(ICoverageMergeService coverageMergeService) : IParserFactory
{
    public ICoverageParser CreateParser(CoverageFormat coverageFormat, Coverage coverage, ILoggerFactory loggerFactory)
    {
        return coverageFormat switch
        {
            CoverageFormat.Cobertura => new CoberturaParser(coverage, loggerFactory.CreateLogger<CoberturaParser>(), coverageMergeService),
            CoverageFormat.SonarQube => new SonarQubeParser(coverage, loggerFactory.CreateLogger<SonarQubeParser>(), coverageMergeService),
            _                        => throw new ArgumentOutOfRangeException(nameof(coverageFormat), "Unknown coverage format")
        };
    }
}