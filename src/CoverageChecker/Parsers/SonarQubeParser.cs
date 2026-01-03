using System.Xml;
using CoverageChecker.Results;
using CoverageChecker.Services;
using CoverageChecker.Utils;
using Microsoft.Extensions.Logging;

namespace CoverageChecker.Parsers;

internal partial class SonarQubeParser(Coverage coverage, ILogger<SonarQubeParser> logger, ICoverageMergeService coverageMergeService) : ParserBase(logger)
{
    protected override void LoadCoverage(XmlReader reader)
    {
        if (!reader.ReadToFollowing("coverage") || reader.Depth != 0)
            throw new CoverageParseException("Expected coverage to be the root element");

        string version = reader.GetRequiredAttribute<string>("version");

        if (version is not "1")
            throw new CoverageParseException("Attribute 'version' on element 'coverage' must be '1'");

        reader.TryEnterElement("coverage", () =>
        {
            reader.ParseElements("file", () =>
            {
                LoadFileCoverage(reader);
            });
        });
    }

    private void LoadFileCoverage(XmlReader reader)
    {
        string filePath = NormalizePath(reader.GetRequiredAttribute<string>("path"));
        LogProcessingFile(filePath);

        FileCoverage file = coverage.GetOrCreateFile(filePath);

        reader.TryEnterElement("file", () =>
        {
            reader.ParseElements("lineToCover", () =>
            {
                LoadLineCoverage(file, reader);
            });
        });
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Processing file: {FilePath}")]
    private partial void LogProcessingFile(string filePath);

    private void LoadLineCoverage(FileCoverage file, XmlReader reader)
    {
        int lineNumber = reader.GetRequiredAttribute<int>("lineNumber");
        bool isCovered = reader.GetRequiredAttribute<bool>("covered");

        int? branches = null;
        if (reader.TryGetAttribute("branchesToCover", out int tempBranches))
            branches = tempBranches;

        int? coveredBranches = null;
        if (reader.TryGetAttribute("coveredBranches", out int tempCoveredBranches))
            coveredBranches = tempCoveredBranches;

        reader.ConsumeElement("lineToCover");

        file.AddOrMergeLine(new LineCoverage(lineNumber, isCovered, branches, coveredBranches), coverageMergeService);
    }
}