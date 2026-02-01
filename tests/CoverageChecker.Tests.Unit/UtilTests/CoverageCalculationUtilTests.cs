using CoverageChecker.Results;
using CoverageChecker.Utils;

namespace CoverageChecker.Tests.Unit.UtilTests;

public class CoverageCalculationUtilFileTests
{
    [Test]
    public void CoverageCalculationUtilsLineCoverageOnLinesFullCoverageReturnsFullCoverage()
    {
        LineCoverage[] lines = CoverageTestData.Lines4Of4Covered;

        double coverage = lines.CalculateCoverage(CoverageType.Line);

        Assert.That(coverage, Is.EqualTo(1));
    }

    [Test]
    public void CoverageCalculationUtilsLineCoverageOnFilesFullCoverageReturnsFullCoverage()
    {
        FileCoverage[] files =
        [
            CoverageTestData.CreateFile(CoverageTestData.Lines4Of4Covered, $"{CoverageTestData.FilePath}-1"),
            CoverageTestData.CreateFile(CoverageTestData.Lines1Of1CoveredWith0Of4Branches, $"{CoverageTestData.FilePath}-2"),
            CoverageTestData.CreateFile(CoverageTestData.Lines3Of3CoveredWith2Of2Branches, $"{CoverageTestData.FilePath}-3")
        ];

        double coverage = files.CalculateCoverage(CoverageType.Line);

        Assert.That(coverage, Is.EqualTo(1));
    }

    [Test]
    public void CoverageCalculationUtilsBranchCoverageOnLinesFullCoverageReturnsFullCoverage()
    {
        LineCoverage[] lines = CoverageTestData.Lines3Of3CoveredWith2Of2Branches;

        double coverage = lines.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo(1));
    }

    [Test]
    public void CoverageCalculationUtilsBranchCoverageOnFilesFullCoverageReturnsFullCoverage()
    {
        FileCoverage[] files =
        [
            CoverageTestData.CreateFile(CoverageTestData.Lines3Of3CoveredWith2Of2Branches, $"{CoverageTestData.FilePath}-1"),
            CoverageTestData.CreateFile(CoverageTestData.Lines3Of3CoveredWith2Of2Branches, $"{CoverageTestData.FilePath}-2")
        ];

        double coverage = files.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo(1));
    }

    [Test]
    public void CoverageCalculationUtilsLineCoverageOnLinesPartialCoverageReturnsPartialCoverage()
    {
        LineCoverage[] lines = CoverageTestData.Lines3Of5Covered;

        double coverage = lines.CalculateCoverage(CoverageType.Line);

        Assert.That(coverage, Is.EqualTo((double)3 / 5));
    }

    [Test]
    public void CoverageCalculationUtilsLineCoverageOnFilesPartialCoverageReturnsPartialCoverage()
    {
        FileCoverage[] files =
        [
            CoverageTestData.CreateFile(CoverageTestData.Lines0Of3Covered, $"{CoverageTestData.FilePath}-1"),
            CoverageTestData.CreateFile(CoverageTestData.Lines3Of5Covered, $"{CoverageTestData.FilePath}-2"),
            CoverageTestData.CreateFile(CoverageTestData.Lines4Of4Covered, $"{CoverageTestData.FilePath}-3")
        ];

        double coverage = files.CalculateCoverage(CoverageType.Line);

        Assert.That(coverage, Is.EqualTo((double)7 / 12));
    }

    [Test]
    public void CoverageCalculationUtilsBranchCoverageOnLinesPartialCoverageReturnsPartialCoverage()
    {
        LineCoverage[] lines = CoverageTestData.Lines2Of3CoveredWith3Of4Branches;

        double coverage = lines.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo((double)3 / 4));
    }

    [Test]
    public void CoverageCalculationUtilsBranchCoverageOnFilesPartialCoverageReturnsPartialCoverage()
    {
        FileCoverage[] files =
        [
            CoverageTestData.CreateFile(CoverageTestData.Lines3Of5Covered, $"{CoverageTestData.FilePath}-1"),
            CoverageTestData.CreateFile(CoverageTestData.Lines1Of1CoveredWith0Of4Branches, $"{CoverageTestData.FilePath}-2"),
            CoverageTestData.CreateFile(CoverageTestData.Lines2Of3CoveredWith3Of4Branches, $"{CoverageTestData.FilePath}-3"),
            CoverageTestData.CreateFile(CoverageTestData.Lines3Of3CoveredWith2Of2Branches, $"{CoverageTestData.FilePath}-4")
        ];

        double coverage = files.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo((double)5 / 10));
    }

    [Test]
    public void CoverageCalculationUtilsLineCoverageOnLinesNoCoverageReturnsNoCoverage()
    {
        LineCoverage[] lines = CoverageTestData.Lines0Of3Covered;

        double coverage = lines.CalculateCoverage(CoverageType.Line);

        Assert.That(coverage, Is.EqualTo(0));
    }

    [Test]
    public void CoverageCalculationUtilsLineCoverageOnFilesNoCoverageReturnsNoCoverage()
    {
        FileCoverage[] files =
        [
            CoverageTestData.CreateFile(CoverageTestData.Lines0Of3Covered, $"{CoverageTestData.FilePath}-1"),
            CoverageTestData.CreateFile(CoverageTestData.Lines0Of3Covered, $"{CoverageTestData.FilePath}-2"),
            CoverageTestData.CreateFile(CoverageTestData.Lines0Of3Covered, $"{CoverageTestData.FilePath}-3")
        ];

        double coverage = files.CalculateCoverage(CoverageType.Line);

        Assert.That(coverage, Is.EqualTo(0));
    }

    [Test]
    public void CoverageCalculationUtilsBranchCoverageOnLinesNoCoverageReturnsNoCoverage()
    {
        LineCoverage[] lines = CoverageTestData.Lines1Of1CoveredWith0Of4Branches;

        double coverage = lines.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo(0));
    }

    [Test]
    public void CoverageCalculationUtilsBranchCoverageOnFilesNoCoverageReturnsNoCoverage()
    {
        FileCoverage[] files =
        [
            CoverageTestData.CreateFile(CoverageTestData.Lines3Of5Covered, $"{CoverageTestData.FilePath}-1"),
            CoverageTestData.CreateFile(CoverageTestData.Lines1Of1CoveredWith0Of4Branches, $"{CoverageTestData.FilePath}-2")
        ];

        double coverage = files.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo(0));
    }

    [Test]
    public void CoverageCalculationUtilsLineCoverageOnLinesEmptyCoverageReturnsNaNCoverage()
    {
        LineCoverage[] lines = CoverageTestData.LinesEmpty;

        double coverage = lines.CalculateCoverage(CoverageType.Line);

        Assert.That(coverage, Is.NaN);
    }

    [Test]
    public void CoverageCalculationUtilsLineCoverageOnFilesEmptyCoverage1ReturnsNaNCoverage()
    {
        FileCoverage[] files = CoverageTestData.FilesEmpty;

        double coverage = files.CalculateCoverage(CoverageType.Line);

        Assert.That(coverage, Is.NaN);
    }

    [Test]
    public void CoverageCalculationUtilsLineCoverageOnFilesEmptyCoverage2ReturnsNaNCoverage()
    {
        FileCoverage[] files =
        [
            CoverageTestData.CreateFile(CoverageTestData.LinesEmpty, $"{CoverageTestData.FilePath}-1"),
            CoverageTestData.CreateFile(CoverageTestData.LinesEmpty, $"{CoverageTestData.FilePath}-2")
        ];

        double coverage = files.CalculateCoverage(CoverageType.Line);

        Assert.That(coverage, Is.NaN);
    }

    [Test]
    public void CoverageCalculationUtilsBranchCoverageOnLinesEmptyCoverageReturnsNaNCoverage()
    {
        LineCoverage[] lines = CoverageTestData.Lines3Of5Covered;

        double coverage = lines.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.NaN);
    }


    [Test]
    public void CoverageCalculationUtilsBranchCoverageOnFilesEmptyCoverageReturnsNaNCoverage()
    {
        FileCoverage[] files =
        [
            CoverageTestData.CreateFile(CoverageTestData.Lines0Of3Covered, $"{CoverageTestData.FilePath}-1"),
            CoverageTestData.CreateFile(CoverageTestData.Lines4Of4Covered, $"{CoverageTestData.FilePath}-2")
        ];

        double coverage = files.CalculateCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.NaN);
    }

    [Test]
    public void CoverageCalculationUtilsInvalidCoverageOnLinesThrowsException()
    {
        LineCoverage[] lines = CoverageTestData.Lines3Of5Covered;

        Exception e = Assert.Throws<ArgumentOutOfRangeException>(() => lines.CalculateCoverage((CoverageType)3));
        Assert.That(e.Message, Is.EqualTo($"Unknown coverage type (Parameter 'coverageType'){Environment.NewLine}Actual value was 3."));
    }

    [Test]
    public void CoverageCalculationUtilsInvalidCoverageOnFilesThrowsException()
    {
        FileCoverage[] files =
        [
            CoverageTestData.CreateFile(CoverageTestData.Lines0Of3Covered, $"{CoverageTestData.FilePath}-1"),
            CoverageTestData.CreateFile(CoverageTestData.Lines3Of3CoveredWith2Of2Branches, $"{CoverageTestData.FilePath}-2")
        ];

        Exception e = Assert.Throws<ArgumentOutOfRangeException>(() => files.CalculateCoverage((CoverageType)2));
        Assert.That(e.Message, Is.EqualTo($"Unknown coverage type (Parameter 'coverageType'){Environment.NewLine}Actual value was 2."));
    }
}