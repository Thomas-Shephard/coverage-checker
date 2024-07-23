﻿using CoverageChecker.Parsers;
using CoverageChecker.Results;

namespace CoverageChecker.EndToEndTests.ParserTests;

public class CoberturaParserTests {
    private readonly string _directory = Path.Combine(TestContext.CurrentContext.TestDirectory, "CoverageFiles", "Cobertura");

    [Test]
    public void CoberturaParser_LoadCoverage_FullLineCoverage_ReturnsCoverage() {
        CoberturaParser coberturaParser = new(_directory, "FullLineCoverage.xml");

        Coverage coverage = coberturaParser.LoadCoverage();

        Assert.Multiple(() => {
            Assert.That(coverage.Files, Has.Count.EqualTo(3));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(6));
            Assert.That(coverage.Files[1].Lines, Has.Count.EqualTo(3));
            Assert.That(coverage.Files[2].Lines, Has.Count.EqualTo(2));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(1));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.EqualTo((double)5 / 6));
        });
    }

    [Test]
    public void CoberturaParser_LoadCoverage_FullBranchCoverage_ReturnsCoverage() {
        CoberturaParser coberturaParser = new(_directory, "FullBranchCoverage.xml");

        Coverage coverage = coberturaParser.LoadCoverage();

        Assert.Multiple(() => {
            Assert.That(coverage.Files, Has.Count.EqualTo(2));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(5));
            Assert.That(coverage.Files[1].Lines, Has.Count.EqualTo(2));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(1));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.EqualTo(1));
        });
    }

    [Test]
    public void CoberturaParser_LoadCoverage_PartialLineCoverage_ReturnsCoverage() {
        CoberturaParser coberturaParser = new(_directory, "PartialLineCoverage.xml");

        Coverage coverage = coberturaParser.LoadCoverage();

        Assert.Multiple(() => {
            Assert.That(coverage.Files, Has.Count.EqualTo(1));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(5));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo((double)1 / 5));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        });
    }

    [Test]
    public void CoberturaParser_LoadCoverage_NoPackages_ReturnsCoverage() {
        CoberturaParser coberturaParser = new(_directory, "NoPackages.xml");

        Coverage coverage = coberturaParser.LoadCoverage();

        Assert.Multiple(() => {
            Assert.That(coverage.Files, Is.Empty);
            Assert.That(coverage.CalculateOverallCoverage(), Is.NaN);
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        });
    }

    [Test]
    public void CoberturaParser_LoadCoverage_NoClasses_ReturnsCoverage() {
        CoberturaParser coberturaParser = new(_directory, "NoClasses.xml");

        Coverage coverage = coberturaParser.LoadCoverage();

        Assert.Multiple(() => {
            Assert.That(coverage.Files, Is.Empty);
            Assert.That(coverage.CalculateOverallCoverage(), Is.NaN);
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        });
    }

    [Test]
    public void CoberturaParser_LoadCoverage_NoLines_ReturnsCoverage() {
        CoberturaParser coberturaParser = new(_directory, "NoLines.xml");

        Coverage coverage = coberturaParser.LoadCoverage();

        Assert.Multiple(() => {
            Assert.That(coverage.Files, Has.Count.EqualTo(2));
            Assert.That(coverage.Files[0].Lines, Is.Empty);
            Assert.That(coverage.Files[1].Lines, Is.Empty);
            Assert.That(coverage.CalculateOverallCoverage(), Is.NaN);
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        });
    }

    [Test]
    public void CoberturaParser_LoadCoverage_InvalidBranchCoverage1_ThrowsCoverageParseException() {
        CoberturaParser coberturaParser = new(_directory, "InvalidBranchCoverage1.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coberturaParser.LoadCoverage());
        Assert.That(e.Message, Is.EqualTo("Attribute 'condition-coverage' on element 'line' is not in the correct format"));
    }

    [Test]
    public void CoberturaParser_LoadCoverage_InvalidBranchCoverage2_ThrowsCoverageParseException() {
        CoberturaParser coberturaParser = new(_directory, "InvalidBranchCoverage2.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coberturaParser.LoadCoverage());
        Assert.That(e.Message, Is.EqualTo("Attribute 'condition-coverage' on element 'line' is not in the correct format"));
    }

    [Test]
    public void CoberturaParser_LoadCoverage_InvalidBranchCoverage3_ThrowsCoverageParseException() {
        CoberturaParser coberturaParser = new(_directory, "InvalidBranchCoverage3.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coberturaParser.LoadCoverage());
        Assert.That(e.Message, Is.EqualTo("Attribute 'condition-coverage' on element 'line' is not in the correct format"));
    }

    [Test]
    public void CoberturaParser_LoadCoverage_EmptyFile_ThrowsCoverageParseException() {
        CoberturaParser coberturaParser = new(_directory, "EmptyFile.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coberturaParser.LoadCoverage());
        Assert.That(e.Message, Is.EqualTo("Failed to load coverage file"));
    }

    [Test]
    public void CoberturaParser_LoadCoverage_InvalidFile_ThrowsCoverageParseException() {
        CoberturaParser coberturaParser = new(_directory, "InvalidFile.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coberturaParser.LoadCoverage());
        Assert.That(e.Message, Is.EqualTo("No coverage files found"));
    }
}