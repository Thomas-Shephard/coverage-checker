using CoverageChecker.Results;
using CoverageChecker.Utils;

namespace CoverageChecker.Tests.UtilTests.CoverageUtilTests;

public class CoverageFileUtilTests {
    [Test]
    public void CalculateCoverageUtils_Branch_FileCoverage_FullCoverage_ReturnsCoverage() {
        FileCoverage[] files = [
            new FileCoverage([
                new LineCoverage(1, true, 3, 3),
                new LineCoverage(2, true, 4, 4)
            ], "coverage-file-1"),
            new FileCoverage([
                new LineCoverage(1, true, 2, 2),
                new LineCoverage(2, true),
                new LineCoverage(3, true)
            ], "coverage-file-2")
        ];

        double coverage = files.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo(1));
    }

    [Test]
    public void CalculateCoverageUtils_Branch_LineCoverage_FullCoverage_ReturnsCoverage() {
        LineCoverage[] lines = [
            new LineCoverage(1, true, 3, 3),
            new LineCoverage(2, true, 4, 4),
            new LineCoverage(3, true, 2, 2),
            new LineCoverage(4, true),
            new LineCoverage(5, true)
        ];

        double coverage = lines.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo(1));
    }

    [Test]
    public void CalculateCoverageUtils_Branch_FileCoverage_PartialCoverage_ReturnsCoverage() {
        FileCoverage[] files = [
            new FileCoverage([
                new LineCoverage(1, true, 3, 3),
                new LineCoverage(2, true, 4, 4)
            ], "coverage-file-1"),
            new FileCoverage([
                new LineCoverage(1, true, 2, 1)
            ], "coverage-file-2")
        ];

        double coverage = files.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo((double) 8 / 9));
    }

    [Test]
    public void CalculateCoverageUtils_Branch_LineCoverage_PartialCoverage_ReturnsCoverage() {
        LineCoverage[] lines = [
            new LineCoverage(1, true, 3, 3),
            new LineCoverage(2, true, 2, 2),
            new LineCoverage(3, true, 2, 1)
        ];

        double coverage = lines.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo((double) 6 / 7));
    }

    [Test]
    public void CalculateCoverageUtils_Branch_FileCoverage_NoCoverage_ReturnsCoverage() {
        FileCoverage[] files = [
            new FileCoverage([
                new LineCoverage(1, false, 4, 0),
                new LineCoverage(2, false, 4, 0)
            ], "coverage-file-1"),
            new FileCoverage([
                new LineCoverage(1, false, 2, 0)
            ], "coverage-file-2")
        ];

        double coverage = files.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo(0));
    }

    [Test]
    public void CalculateCoverageUtils_Branch_LineCoverage_NoCoverage_ReturnsCoverage() {
        LineCoverage[] lines = [
            new LineCoverage(1, false, 4, 0),
            new LineCoverage(2, false, 8, 0),
            new LineCoverage(3, false, 2, 0)
        ];

        double coverage = lines.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo(0));
    }

    [Test]
    public void CalculateCoverageUtils_Branch_FileCoverage_EmptyCoverage_ReturnsCoverage() {
        FileCoverage[] files = [
            new FileCoverage([
                new LineCoverage(1, true),
                new LineCoverage(2, true)
            ], "coverage-file-1"),
            new FileCoverage([
                new LineCoverage(1, false)
            ], "coverage-file-2")
        ];

        double coverage = files.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.NaN);
    }

    [Test]
    public void CalculateCoverageUtils_Branch_LineCoverage_EmptyCoverage_ReturnsCoverage() {
        LineCoverage[] lines = [
            new LineCoverage(1, true),
            new LineCoverage(2, true),
            new LineCoverage(3, false)
        ];

        double coverage = lines.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.NaN);
    }
}