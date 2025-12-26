using CoverageChecker.Results;

namespace CoverageChecker.Parsers;

internal class ParserFactory : IParserFactory
{
    public ICoverageParser CreateParser(CoverageFormat coverageFormat, Coverage coverage)
    {
        return coverageFormat switch
        {
            CoverageFormat.Cobertura => new CoberturaParser(coverage),
            CoverageFormat.SonarQube => new SonarQubeParser(coverage),
            _                        => throw new ArgumentOutOfRangeException(nameof(coverageFormat), "Unknown coverage format")
        };
    }
}