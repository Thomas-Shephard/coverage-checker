using CoverageChecker.Results;
using Microsoft.Extensions.Logging;

namespace CoverageChecker.Parsers;

internal class ParserFactory : IParserFactory
{
    public ICoverageParser CreateParser(CoverageFormat coverageFormat, Coverage coverage, ILoggerFactory loggerFactory)
    {
        return coverageFormat switch
        {
            CoverageFormat.Cobertura => new CoberturaParser(coverage, loggerFactory.CreateLogger<CoberturaParser>()),
            CoverageFormat.SonarQube => new SonarQubeParser(coverage, loggerFactory.CreateLogger<SonarQubeParser>()),
            _                        => throw new ArgumentOutOfRangeException(nameof(coverageFormat), "Unknown coverage format")
        };
    }
}