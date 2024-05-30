using System.Xml.Linq;
using CoverageChecker.Results;
using CoverageChecker.Utils;

namespace CoverageChecker.Parsers;

public class CoberturaParser(IEnumerable<string> globPatterns, string? directory = null) : BaseParser(globPatterns, directory) {
    public CoberturaParser(string globPattern, string? directory = null) : this([globPattern], directory) { }

    protected override FileCoverage[] LoadCoverageFile(XDocument coverageFile) {
        // Select the coverage element
        XElement coverageElement = coverageFile.GetRequiredElement("coverage");

        // Select the version attribute and ensure that it is '1.9'
        if (coverageElement.GetRequiredAttribute("version").Value is not "1.9")
            throw new CoverageParseException("Attribute 'version' on element 'coverage' must be '1.9'");

        // Select the packages element
        XElement packagesElement = coverageElement.GetRequiredElement("packages");

        // Create a FileCoverage array for each package element
        return packagesElement.Elements("package")
                              .SelectMany(CreatePackageCoverage)
                              .ToArray();
    }

    private static FileCoverage[] CreatePackageCoverage(XElement packageElement) {
        // Select the name attribute
        string packageName = packageElement.GetRequiredAttribute("name").Value;

        // Select the classes element
        XElement classesElement = packageElement.GetRequiredElement("classes");

        // Create a FileCoverage object for each class element
        return classesElement.Elements("class")
                             .Select(classElement => CreateClassCoverage(classElement, packageName))
                             .ToArray();
    }

    private static FileCoverage CreateClassCoverage(XElement classElement, string packageName) {
        // Select the filename and name attributes
        string path = classElement.GetRequiredAttribute("filename").Value;
        string className = classElement.GetRequiredAttribute("name").Value;

        // Select the methods and lines elements
        XElement methodsElement = classElement.GetRequiredElement("methods");
        XElement linesElement = classElement.GetRequiredElement("lines");

        // Create a LineCoverage array for each method element and a LineCoverage object for each line element
        LineCoverage[] lines = methodsElement.Elements("method")
                                             .SelectMany(methodElement => CreateMethodCoverage(methodElement, className))
                                             .Concat(linesElement.Elements("line").Select(lineElement => CreateLineCoverage(lineElement, className)))
                                             .ToArray();

        return new FileCoverage(lines, path, packageName);
    }

    private static LineCoverage[] CreateMethodCoverage(XElement methodElement, string className) {
        // Select the name and signature attributes
        string methodName = methodElement.GetRequiredAttribute("name").Value;
        string methodSignature = methodElement.GetRequiredAttribute("signature").Value;

        // Select the lines element
        XElement linesElement = methodElement.GetRequiredElement("lines");

        // Create a LineCoverage object for each line element
        return linesElement.Elements("line")
                           .Select(lineElement => CreateLineCoverage(lineElement, className, methodName, methodSignature))
                           .ToArray();
    }

    private static LineCoverage CreateLineCoverage(XElement lineElement, string className, string? methodName = null, string? methodSignature = null) {
        // Select the number, hits, and branch attributes
        int lineNumber = lineElement.ParseRequiredAttribute<int>("number");
        bool isCovered = lineElement.ParseRequiredAttribute<int>("hits") > 0;
        bool hasBranches = lineElement.ParseOptionalAttribute<bool>("branch") ?? false;

        if (!hasBranches) {
            return new LineCoverage(lineNumber, isCovered, className: className, methodName: methodName, methodSignature: methodSignature);
        }

        // Select the condition-coverage attribute
        string conditionCoverage = lineElement.GetRequiredAttribute("condition-coverage").Value;
        (int branches, int coveredBranches) = ParseConditionCoverage(conditionCoverage);

        return new LineCoverage(lineNumber, isCovered, branches, coveredBranches, className, methodName, methodSignature);
    }

    private static (int branches, int coveredBranches) ParseConditionCoverage(string conditionCoverage) {
        const string conditionCoverageInvalidMessage = "Attribute 'condition-coverage' on element 'line' is not in the correct format";
        // The condition-coverage attribute is formatted as "x% (y/z)"
        // x is the percentage of branches covered, y is the number of covered branches, and z is the number of branches

        string[] conditionCoverageValues;

        try {
            // Retrieve the number of covered branches and the number of branches from the condition-coverage attribute
            conditionCoverageValues = conditionCoverage.Split(" ")[1].TrimStart('(').TrimEnd(')').Split('/');
        } catch (IndexOutOfRangeException) {
            throw new CoverageParseException(conditionCoverageInvalidMessage);
        }

        // Ensure that only 2 values were found (number of covered branches and number of branches)
        if (conditionCoverageValues.Length != 2) {
            throw new CoverageParseException(conditionCoverageInvalidMessage);
        }

        // Ensure that the number of covered branches and the number of branches are integers
        if (!int.TryParse(conditionCoverageValues[0], out int coveredBranches) ||
            !int.TryParse(conditionCoverageValues[1], out int branches)) {
            throw new CoverageParseException(conditionCoverageInvalidMessage);
        }

        return (branches, coveredBranches);
    }
}