using CoverageChecker.Results;
using CoverageChecker.Utils;

namespace CoverageChecker.Tests.UtilTests.CoverageUtilTests;

public class CoverageFileUtilTests {
    [Test]
    public void CalculateCoverageUtils_Branch_FileCoverage_FullCoverage_ReturnsCoverage() {
        FileCoverage[] files = [
            new FileCoverage([
                new LineCoverage(true, 3, 3),
                new LineCoverage(true, 4, 4)
            ]),
            new FileCoverage([
                new LineCoverage(true, 2, 2),
                new LineCoverage(true),
                new LineCoverage(true)
            ])
        ];

        double coverage = files.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo(1));
    }

    [Test]
    public void CalculateCoverageUtils_Branch_LineCoverage_FullCoverage_ReturnsCoverage() {
        LineCoverage[] lines = [
            new LineCoverage(true, 3, 3),
            new LineCoverage(true, 4, 4),
            new LineCoverage(true, 2, 2),
            new LineCoverage(true),
            new LineCoverage(true)
        ];

        double coverage = lines.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo(1));
    }

    [Test]
    public void CalculateCoverageUtils_Branch_FileCoverage_PartialCoverage_ReturnsCoverage() {
        FileCoverage[] files = [
            new FileCoverage([
                new LineCoverage(true, 3, 3),
                new LineCoverage(true, 4, 4)
            ]),
            new FileCoverage([
                new LineCoverage(true, 2, 1)
            ])
        ];

        double coverage = files.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo((double) 8 / 9));
    }

    [Test]
    public void CalculateCoverageUtils_Branch_LineCoverage_PartialCoverage_ReturnsCoverage() {
        LineCoverage[] lines = [
            new LineCoverage(true, 3, 3),
            new LineCoverage(true, 2, 2),
            new LineCoverage(true, 2, 1)
        ];

        double coverage = lines.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo((double) 6 / 7));
    }

    [Test]
    public void CalculateCoverageUtils_Branch_FileCoverage_NoCoverage_ReturnsCoverage() {
        FileCoverage[] files = [
            new FileCoverage([
                new LineCoverage(false, 4, 0),
                new LineCoverage(false, 4, 0)
            ]),
            new FileCoverage([
                new LineCoverage(false, 2, 0)
            ])
        ];

        double coverage = files.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo(0));
    }

    [Test]
    public void CalculateCoverageUtils_Branch_LineCoverage_NoCoverage_ReturnsCoverage() {
        LineCoverage[] lines = [
            new LineCoverage(false, 4, 0),
            new LineCoverage(false, 8, 0),
            new LineCoverage(false, 2, 0)
        ];

        double coverage = lines.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo(0));
    }

    [Test]
    public void CalculateCoverageUtils_Branch_FileCoverage_EmptyCoverage_ReturnsCoverage() {
        FileCoverage[] files = [
            new FileCoverage([
                new LineCoverage(true),
                new LineCoverage(true)
            ]),
            new FileCoverage([
                new LineCoverage(false)
            ])
        ];

        double coverage = files.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.NaN);
    }

    [Test]
    public void CalculateCoverageUtils_Branch_LineCoverage_EmptyCoverage_ReturnsCoverage() {
        LineCoverage[] lines = [
            new LineCoverage(true),
            new LineCoverage(true),
            new LineCoverage(false)
        ];

        double coverage = lines.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.NaN);
    }
}