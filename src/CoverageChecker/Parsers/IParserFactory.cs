using CoverageChecker.Results;
using Microsoft.Extensions.Logging;

namespace CoverageChecker.Parsers;

internal interface IParserFactory
{
    ICoverageParser CreateParser(CoverageFormat coverageFormat, Coverage coverage, ILoggerFactory loggerFactory);
    CoverageFormat DetectFormat(string filePath);
}
