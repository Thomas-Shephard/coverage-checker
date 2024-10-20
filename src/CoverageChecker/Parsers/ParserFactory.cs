using CoverageChecker.Results;

namespace CoverageChecker.Parsers;

internal static class ParserFactory {
    internal static BaseParser CreateParser(CoverageFormat coverageFormat, Coverage coverage) => coverageFormat switch {
        CoverageFormat.Cobertura => new CoberturaParser(coverage),
        CoverageFormat.SonarQube => new SonarQubeParser(coverage),
        _                        => throw new ArgumentOutOfRangeException(nameof(coverageFormat), "Unknown coverage format")
    };
}