using System.Xml;
using CoverageChecker.Results;
using CoverageChecker.Utils;

namespace CoverageChecker.Parsers;

internal class SonarQubeParser(Coverage coverage) : BaseParser {
    protected override void LoadCoverage(XmlReader reader) {
        reader.Read();

        string version = reader.GetRequiredAttribute<string>("version");

        if (version is not "1")
            throw new CoverageParseException("Attribute 'version' on element 'coverage' must be '1'");

        reader.TryEnterElement("coverage", () => {
            reader.ParseElements("file", () => {
                LoadFileCoverage(reader);
            });
        });
    }

    private void LoadFileCoverage(XmlReader reader) {
        string filePath = reader.GetRequiredAttribute<string>("path");

        FileCoverage file = coverage.GetOrCreateFile(filePath);

        reader.TryEnterElement("file", () => {
            reader.ParseElements("lineToCover", () => {
                LoadLineCoverage(file, reader);
            });
        });
    }

    private static void LoadLineCoverage(FileCoverage file, XmlReader reader) {
        int lineNumber = reader.GetRequiredAttribute<int>("lineNumber");
        bool isCovered = reader.GetRequiredAttribute<bool>("covered");

        int? branches = reader.GetOptionalAttribute<int>("branchesToCover");
        int? coveredBranches = reader.GetOptionalAttribute<int>("coveredBranches");

        reader.ConsumeElement(reader.Depth, "lineToCover", reader.IsEmptyElement);

        file.AddLine(lineNumber, isCovered, branches, coveredBranches);
    }
}