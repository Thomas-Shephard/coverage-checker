using CoverageChecker.Results;
using CoverageChecker.Utils;

namespace CoverageChecker.Tests.UtilTests.CoverageUtilTests;

public class CoverageLineUtilTests {
    [Test]
    public void CalculateCoverageUtils_Line_FileCoverage_FullCoverage_ReturnsCoverage() {
        FileCoverage[] files = [
            new FileCoverage([
                new LineCoverage(1, true),
                new LineCoverage(2, true)
            ], "coverage-file-1"),
            new FileCoverage([
                new LineCoverage(1, true),
                new LineCoverage(2, true),
                new LineCoverage(3, true)
            ], "coverage-file-2")
        ];


        double coverage = files.CalculateCoverage(CoverageType.Line);

        Assert.That(coverage, Is.EqualTo(1));
    }

    [Test]
    public void CalculateCoverageUtils_Line_LineCoverage_FullCoverage_ReturnsCoverage() {
        LineCoverage[] lines = [
            new LineCoverage(1, true),
            new LineCoverage(2, true),
            new LineCoverage(3, true),
            new LineCoverage(4, true),
            new LineCoverage(5, true)
        ];

        double coverage = lines.CalculateCoverage(CoverageType.Line);

        Assert.That(coverage, Is.EqualTo(1));
    }

    [Test]
    public void CalculateCoverageUtils_Line_FileCoverage_PartialCoverage_ReturnsCoverage() {
        FileCoverage[] files = [
            new FileCoverage([
                new LineCoverage(1, true),
                new LineCoverage(2, true)
            ], "coverage-file-1"),
            new FileCoverage([
                new LineCoverage(1, true),
                new LineCoverage(2, false),
                new LineCoverage(3, false),
                new LineCoverage(4, false)
            ], "coverage-file-2"),
            new FileCoverage([
                new LineCoverage(1, false)
            ], "coverage-file-3")
        ];

        double coverage = files.CalculateCoverage(CoverageType.Line);

        Assert.That(coverage, Is.EqualTo((double)3 / 7));
    }

    [Test]
    public void CalculateCoverageUtils_Line_LineCoverage_PartialCoverage_ReturnsCoverage() {
        LineCoverage[] lines = [
            new LineCoverage(1, true),
            new LineCoverage(2, false),
            new LineCoverage(3, true)
        ];

        double coverage = lines.CalculateCoverage(CoverageType.Line);

        Assert.That(coverage, Is.EqualTo((double)2 / 3));
    }

    [Test]
    public void CalculateCoverageUtils_Line_FileCoverage_NoCoverage_ReturnsCoverage() {
        FileCoverage[] files = [
            new FileCoverage([
                new LineCoverage(1, false)
            ], "coverage-file")
        ];

        double coverage = files.CalculateCoverage(CoverageType.Line);

        Assert.That(coverage, Is.EqualTo(0));
    }

    [Test]
    public void CalculateCoverageUtils_Line_LineCoverage_NoCoverage_ReturnsCoverage() {
        LineCoverage[] lines = [
            new LineCoverage(1, false),
            new LineCoverage(2, false)
        ];

        double coverage = lines.CalculateCoverage(CoverageType.Line);

        Assert.That(coverage, Is.EqualTo(0));
    }

    [Test]
    public void CalculateCoverageUtils_Line_FileCoverage_EmptyCoverage_ReturnsCoverage() {
        FileCoverage[] files = [];

        double coverage = files.CalculateCoverage(CoverageType.Line);

        Assert.That(coverage, Is.NaN);
    }

    [Test]
    public void CalculateCoverageUtils_Line_LineCoverage_EmptyCoverage_ReturnsCoverage() {
        LineCoverage[] lines = [];

        double coverage = lines.CalculateCoverage(CoverageType.Line);

        Assert.That(coverage, Is.NaN);
    }
}