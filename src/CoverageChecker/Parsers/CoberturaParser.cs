﻿using System.Xml.Linq;
using CoverageChecker.Results;
using CoverageChecker.Utils;

namespace CoverageChecker.Parsers;

internal class CoberturaParser(Coverage coverage) : BaseParser {
    protected override void LoadCoverage(XDocument coverageDocument) {
        XElement coverageElement = coverageDocument.GetRequiredElement("coverage");

        XElement packagesElement = coverageElement.GetRequiredElement("packages");
        foreach (XElement packageElement in packagesElement.Elements("package")) {
            LoadPackageCoverage(packageElement);
        }
    }

    private void LoadPackageCoverage(XElement packageElement) {
        string packageName = packageElement.GetRequiredAttribute("name").Value;

        XElement classesElement = packageElement.GetRequiredElement("classes");
        foreach (XElement classElement in classesElement.Elements("class")) {
            LoadClassCoverage(classElement, packageName);
        }
    }

    private void LoadClassCoverage(XElement classElement, string packageName) {
        string filePath = classElement.GetRequiredAttribute("filename").Value;
        string className = classElement.GetRequiredAttribute("name").Value;

        FileCoverage file = coverage.GetOrCreateFile(filePath, packageName);

        XElement methodsElement = classElement.GetRequiredElement("methods");
        foreach (XElement methodElement in methodsElement.Elements("method")) {
            LoadMethodCoverage(file, methodElement, className);
        }

        XElement linesElement = classElement.GetRequiredElement("lines");
        foreach (XElement lineElement in linesElement.Elements("line")) {
            LoadLineCoverage(file, lineElement, className);
        }
    }

    private static void LoadMethodCoverage(FileCoverage file, XElement methodElement, string className) {
        string methodName = methodElement.GetRequiredAttribute("name").Value;
        string? methodSignature = methodElement.Attribute("signature")?.Value;

        XElement linesElement = methodElement.GetRequiredElement("lines");
        foreach (XElement lineElement in linesElement.Elements("line")) {
            LoadLineCoverage(file, lineElement, className, methodName, methodSignature);
        }
    }

    private static void LoadLineCoverage(FileCoverage file, XElement lineElement, string className, string? methodName = null, string? methodSignature = null) {
        int lineNumber = lineElement.ParseRequiredAttribute<int>("number");
        bool isCovered = lineElement.ParseRequiredAttribute<int>("hits") > 0;
        bool hasBranches = lineElement.ParseOptionalAttribute<bool>("branch") ?? false;

        if (!hasBranches) {
            file.AddLine(lineNumber, isCovered, className: className, methodName: methodName, methodSignature: methodSignature);
            return;
        }

        // Select the condition-coverage attribute
        string conditionCoverage = lineElement.GetRequiredAttribute("condition-coverage").Value;
        (int branches, int coveredBranches) = ParseConditionCoverage(conditionCoverage);

        file.AddLine(lineNumber, isCovered, branches, coveredBranches, className, methodName, methodSignature);
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