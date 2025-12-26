using CoverageChecker.Results;

namespace CoverageChecker.Parsers;

internal interface IParserFactory
{
    ICoverageParser CreateParser(CoverageFormat coverageFormat, Coverage coverage);
}
