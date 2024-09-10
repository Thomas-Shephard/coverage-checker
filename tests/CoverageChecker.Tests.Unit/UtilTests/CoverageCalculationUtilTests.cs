using CoverageChecker.Results;
using CoverageChecker.Utils;

namespace CoverageChecker.Tests.Unit.UtilTests;

public class CoverageCalculationUtilFileTests {
    [Test]
    public void CoverageCalculationUtils_LineCoverageOnLines_FullCoverage_ReturnsFullCoverage() {
        LineCoverage[] lines = CoverageTestData.Lines4Of4Covered;

        double coverage = lines.CalculateCoverage(CoverageType.Line);

        Assert.That(coverage, Is.EqualTo(1));
    }

    [Test]
    public void CoverageCalculationUtils_LineCoverageOnFiles_FullCoverage_ReturnsFullCoverage() {
        FileCoverage[] files = [
            new FileCoverage(CoverageTestData.Lines4Of4Covered, $"{CoverageTestData.FilePath}-1"),
            new FileCoverage(CoverageTestData.Lines1Of1CoveredWith0Of4Branches, $"{CoverageTestData.FilePath}-2"),
            new FileCoverage(CoverageTestData.Lines3Of3CoveredWith2Of2Branches, $"{CoverageTestData.FilePath}-3")
        ];

        double coverage = files.CalculateCoverage(CoverageType.Line);

        Assert.That(coverage, Is.EqualTo(1));
    }

    [Test]
    public void CoverageCalculationUtils_BranchCoverageOnLines_FullCoverage_ReturnsFullCoverage() {
        LineCoverage[] lines = CoverageTestData.Lines3Of3CoveredWith2Of2Branches;

        double coverage = lines.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo(1));
    }

    [Test]
    public void CoverageCalculationUtils_BranchCoverageOnFiles_FullCoverage_ReturnsFullCoverage() {
        FileCoverage[] files = [
            new FileCoverage(CoverageTestData.Lines3Of3CoveredWith2Of2Branches, $"{CoverageTestData.FilePath}-1"),
            new FileCoverage(CoverageTestData.Lines3Of3CoveredWith2Of2Branches, $"{CoverageTestData.FilePath}-2")
        ];

        double coverage = files.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo(1));
    }

    [Test]
    public void CoverageCalculationUtils_LineCoverageOnLines_PartialCoverage_ReturnsPartialCoverage() {
        LineCoverage[] lines = CoverageTestData.Lines3Of5Covered;

        double coverage = lines.CalculateCoverage(CoverageType.Line);

        Assert.That(coverage, Is.EqualTo((double)3 / 5));
    }

    [Test]
    public void CoverageCalculationUtils_LineCoverageOnFiles_PartialCoverage_ReturnsPartialCoverage() {
        FileCoverage[] files = [
            new FileCoverage(CoverageTestData.Lines0Of3Covered, $"{CoverageTestData.FilePath}-1"),
            new FileCoverage(CoverageTestData.Lines3Of5Covered, $"{CoverageTestData.FilePath}-2"),
            new FileCoverage(CoverageTestData.Lines4Of4Covered, $"{CoverageTestData.FilePath}-3")
        ];

        double coverage = files.CalculateCoverage(CoverageType.Line);

        Assert.That(coverage, Is.EqualTo((double)7 / 12));
    }

    [Test]
    public void CoverageCalculationUtils_BranchCoverageOnLines_PartialCoverage_ReturnsPartialCoverage() {
        LineCoverage[] lines = CoverageTestData.Lines2Of3CoveredWith3Of4Branches;

        double coverage = lines.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo((double)3 / 4));
    }

    [Test]
    public void CoverageCalculationUtils_BranchCoverageOnFiles_PartialCoverage_ReturnsPartialCoverage() {
        FileCoverage[] files = [
            new FileCoverage(CoverageTestData.Lines3Of5Covered, $"{CoverageTestData.FilePath}-1"),
            new FileCoverage(CoverageTestData.Lines1Of1CoveredWith0Of4Branches, $"{CoverageTestData.FilePath}-2"),
            new FileCoverage(CoverageTestData.Lines2Of3CoveredWith3Of4Branches, $"{CoverageTestData.FilePath}-3"),
            new FileCoverage(CoverageTestData.Lines3Of3CoveredWith2Of2Branches, $"{CoverageTestData.FilePath}-4")
        ];

        double coverage = files.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo((double)5 / 10));
    }

    [Test]
    public void CoverageCalculationUtils_LineCoverageOnLines_NoCoverage_ReturnsNoCoverage() {
        LineCoverage[] lines = CoverageTestData.Lines0Of3Covered;

        double coverage = lines.CalculateCoverage(CoverageType.Line);

        Assert.That(coverage, Is.EqualTo(0));
    }

    [Test]
    public void CoverageCalculationUtils_LineCoverageOnFiles_NoCoverage_ReturnsNoCoverage() {
        FileCoverage[] files = [
            new FileCoverage(CoverageTestData.Lines0Of3Covered, $"{CoverageTestData.FilePath}-1"),
            new FileCoverage(CoverageTestData.Lines0Of3Covered, $"{CoverageTestData.FilePath}-2"),
            new FileCoverage(CoverageTestData.Lines0Of3Covered, $"{CoverageTestData.FilePath}-3")
        ];

        double coverage = files.CalculateCoverage(CoverageType.Line);

        Assert.That(coverage, Is.EqualTo(0));
    }

    [Test]
    public void CoverageCalculationUtils_BranchCoverageOnLines_NoCoverage_ReturnsNoCoverage() {
        LineCoverage[] lines = CoverageTestData.Lines1Of1CoveredWith0Of4Branches;

        double coverage = lines.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo(0));
    }

    [Test]
    public void CoverageCalculationUtils_BranchCoverageOnFiles_NoCoverage_ReturnsNoCoverage() {
        FileCoverage[] files = [
            new FileCoverage(CoverageTestData.Lines3Of5Covered, $"{CoverageTestData.FilePath}-1"),
            new FileCoverage(CoverageTestData.Lines1Of1CoveredWith0Of4Branches, $"{CoverageTestData.FilePath}-2")
        ];

        double coverage = files.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo(0));
    }

    [Test]
    public void CoverageCalculationUtils_LineCoverageOnLines_EmptyCoverage_ReturnsNaNCoverage() {
        LineCoverage[] lines = CoverageTestData.LinesEmpty;

        double coverage = lines.CalculateCoverage(CoverageType.Line);

        Assert.That(coverage, Is.NaN);
    }

    [Test]
    public void CoverageCalculationUtils_LineCoverageOnFiles_EmptyCoverage1_ReturnsNaNCoverage() {
        FileCoverage[] files = CoverageTestData.FilesEmpty;

        double coverage = files.CalculateCoverage(CoverageType.Line);

        Assert.That(coverage, Is.NaN);
    }

    [Test]
    public void CoverageCalculationUtils_LineCoverageOnFiles_EmptyCoverage2_ReturnsNaNCoverage() {
        FileCoverage[] files = [
            new FileCoverage(CoverageTestData.LinesEmpty, $"{CoverageTestData.FilePath}-1"),
            new FileCoverage(CoverageTestData.LinesEmpty, $"{CoverageTestData.FilePath}-2")
        ];

        double coverage = files.CalculateCoverage(CoverageType.Line);

        Assert.That(coverage, Is.NaN);
    }

    [Test]
    public void CoverageCalculationUtils_BranchCoverageOnLines_EmptyCoverage_ReturnsNaNCoverage() {
        LineCoverage[] lines = CoverageTestData.Lines3Of5Covered;

        double coverage = lines.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.NaN);
    }


    [Test]
    public void CoverageCalculationUtils_BranchCoverageOnFiles_EmptyCoverage_ReturnsNaNCoverage() {
        FileCoverage[] files = [
            new FileCoverage(CoverageTestData.Lines0Of3Covered, $"{CoverageTestData.FilePath}-1"),
            new FileCoverage(CoverageTestData.Lines4Of4Covered, $"{CoverageTestData.FilePath}-2")
        ];

        double coverage = files.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.NaN);
    }

    [Test]
    public void CoverageCalculationUtils_InvalidCoverageOnLines_ThrowsException() {
        LineCoverage[] lines = CoverageTestData.Lines3Of5Covered;

        Exception e = Assert.Throws<ArgumentOutOfRangeException>(() => lines.CalculateCoverage((CoverageType)3));
        Assert.That(e.Message, Is.EqualTo($"Unknown coverage type (Parameter 'coverageType'){Environment.NewLine}Actual value was 3."));
    }

    [Test]
    public void CoverageCalculationUtils_InvalidCoverageOnFiles_ThrowsException() {
        FileCoverage[] files = [
            new FileCoverage(CoverageTestData.Lines0Of3Covered, $"{CoverageTestData.FilePath}-1"),
            new FileCoverage(CoverageTestData.Lines3Of3CoveredWith2Of2Branches, $"{CoverageTestData.FilePath}-2")
        ];

        Exception e = Assert.Throws<ArgumentOutOfRangeException>(() => files.CalculateCoverage((CoverageType)2));
        Assert.That(e.Message, Is.EqualTo($"Unknown coverage type (Parameter 'coverageType'){Environment.NewLine}Actual value was 2."));
    }
}