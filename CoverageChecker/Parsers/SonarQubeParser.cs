using System.Xml.Linq;
using CoverageChecker.Results;
using CoverageChecker.Utils;

namespace CoverageChecker.Parsers;

public class SonarQubeParser(IEnumerable<string> globPatterns, string? directory = null) : BaseParser(globPatterns, directory) {
    public SonarQubeParser(string globPattern, string? directory = null) : this([globPattern], directory) { }

    protected override FileCoverage[] LoadCoverageFile(XDocument coverageFile) {
        // Select the coverage element
        XElement coverageElement = coverageFile.GetRequiredElement("coverage");

        // Select the version attribute and ensure that it is '1'
        if (coverageElement.GetRequiredAttribute("version").Value is not "1")
            throw new CoverageParseException("Attribute 'version' on element 'coverage' must be '1'");

        // Create a FileCoverage object for each file element
        return coverageElement.Elements("file")
                              .Select(CreateFileCoverage)
                              .ToArray();
    }

    private static FileCoverage CreateFileCoverage(XElement fileElement) {
        // Select the path attribute
        string path = fileElement.GetRequiredAttribute("path").Value;

        // Create a LineCoverage object for each lineToCover element
        LineCoverage[] lines = fileElement.Elements("lineToCover")
                                          .Select(CreateLineCoverage)
                                          .ToArray();

        return new FileCoverage(lines, path);
    }

    private static LineCoverage CreateLineCoverage(XElement lineToCoverElement) {
        // Select the lineNumber attribute
        int lineNumber = lineToCoverElement.ParseRequiredAttribute<int>("lineNumber");

        // Select the covered attribute
        bool isCovered = lineToCoverElement.ParseRequiredAttribute<bool>("covered");

        // Select the branchesToCover and coveredBranches attributes (these attributes are optional)
        int? branches = lineToCoverElement.ParseOptionalAttribute<int>("branchesToCover");
        int? coveredBranches = lineToCoverElement.ParseOptionalAttribute<int>("coveredBranches");

        return new LineCoverage(lineNumber, isCovered, branches, coveredBranches);
    }
}