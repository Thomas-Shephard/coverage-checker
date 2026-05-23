using System.Xml;
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
            CoverageFormat.OpenCover => new OpenCoverParser(coverage, loggerFactory.CreateLogger<OpenCoverParser>(), coverageMergeService),
            _                        => throw new ArgumentOutOfRangeException(nameof(coverageFormat), "Unknown or unsupported coverage format")
        };
    }

    public CoverageFormat DetectFormat(string filePath)
    {
        try
        {
            using XmlReader reader = XmlReader.Create(filePath, ParserBase.XmlReaderSettings);
            if (reader.MoveToContent() != XmlNodeType.Element || reader.Depth != 0)
            {
                throw new CoverageParseException($"Could not find supported coverage root element in file: {filePath}");
            }

            if (reader.Name == "CoverageSession")
            {
                return CoverageFormat.OpenCover;
            }

            if (reader.Name != "coverage")
            {
                throw new CoverageParseException($"Could not find root 'coverage' element in file: {filePath}");
            }

            if (reader.GetAttribute("version") == "1")
            {
                return CoverageFormat.SonarQube;
            }

            return DetectFromChildElements(reader) ?? throw new CoverageParseException($"Could not auto-detect coverage format for file: {filePath}");
        }
        catch (Exception ex) when (ex is not CoverageException)
        {
            throw new CoverageParseException($"Could not auto-detect coverage format for file: {filePath}", ex);
        }
    }

    private static CoverageFormat? DetectFromChildElements(XmlReader reader)
    {
        if (reader.IsEmptyElement)
        {
            return null;
        }

        int rootDepth = reader.Depth;
        while (reader.Read())
        {
            if (reader.Depth <= rootDepth)
            {
                break;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            switch (reader.Name)
            {
                case "file" or "lineToCover":
                    return CoverageFormat.SonarQube;
                case "packages" or "sources":
                    return CoverageFormat.Cobertura;
            }
        }

        return null;
    }
}
