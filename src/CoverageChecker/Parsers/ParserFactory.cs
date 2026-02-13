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
            _                        => throw new ArgumentOutOfRangeException(nameof(coverageFormat), "Unknown or unsupported coverage format")
        };
    }

    public CoverageFormat DetectFormat(string filePath)
    {
        using StreamReader reader = new(filePath);
        char[] buffer = new char[1024];
        int read = reader.Read(buffer, 0, buffer.Length);
        string content = new(buffer, 0, read);

        if (content.Contains("<coverage", StringComparison.OrdinalIgnoreCase))
        {
            // SonarQube generic coverage uses <coverage version="1">
            if (content.Contains("version=\"1\"", StringComparison.OrdinalIgnoreCase))
            {
                return CoverageFormat.SonarQube;
            }

            return CoverageFormat.Cobertura;
        }

        throw new CoverageParseException($"Could not auto-detect coverage format for file: {filePath}");
    }
}