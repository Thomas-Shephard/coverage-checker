using CoverageChecker.Results;

namespace CoverageChecker.Tests.Unit.ResultTests;

public class CoverageTests
{
    [Test]
    public void Coverage_GetOrCreateFile_ReturnsFileIfExists()
    {
        Coverage coverage = new();

        FileCoverage coverageFile1 = coverage.GetOrCreateFile($"{CoverageTestData.FilePath}-1");

        coverageFile1.AddLine(1, true, 1, 0);
        coverageFile1.AddLine(2, false, 8, 1);
        coverageFile1.AddLine(3, true, 2, 1);

        FileCoverage coverageFile2 = coverage.GetOrCreateFile($"{CoverageTestData.FilePath}-2");

        coverageFile2.AddLine(1, true, 1, 0);
        coverageFile2.AddLine(2, true, 3, 2);
        coverageFile2.AddLine(3, false, 4, 3);

        Assert.Multiple(() =>
        {
            Assert.That(coverage.GetOrCreateFile($"{CoverageTestData.FilePath}-1"), Is.EqualTo(coverageFile1));
            Assert.That(coverage.GetOrCreateFile($"{CoverageTestData.FilePath}-2"), Is.EqualTo(coverageFile2));
            Assert.That(coverage.GetOrCreateFile($"{CoverageTestData.FilePath}-3").Lines, Is.Empty);
        });
    }

    [Test]
    public void Coverage_CalculateOverallCoverage_LineCoverage_ReturnsCoverage()
    {
        Coverage coverage = new();

        FileCoverage coverageFile1 = coverage.GetOrCreateFile($"{CoverageTestData.FilePath}-1");

        coverageFile1.AddLine(1, false);
        coverageFile1.AddLine(2, true);
        coverageFile1.AddLine(3, true);
        coverageFile1.AddLine(4, false);
        coverageFile1.AddLine(5, true);

        FileCoverage coverageFile2 = coverage.GetOrCreateFile($"{CoverageTestData.FilePath}-2");

        coverageFile2.AddLine(1, true);
        coverageFile2.AddLine(2, false);
        coverageFile2.AddLine(3, true);


        FileCoverage[] files =
        [
            new(CoverageTestData.Lines3Of5Covered, $"{CoverageTestData.FilePath}-1"),
            new(CoverageTestData.Lines4Of4Covered, $"{CoverageTestData.FilePath}-2")
        ];

        Coverage coverage = new(files);

        double overallCoverage = coverage.CalculateOverallCoverage();

        Assert.That(overallCoverage, Is.EqualTo((double)7 / 9));
    }

    [Test]
    public void Coverage_CalculateOverallCoverage_BranchCoverage_ReturnsCoverage()
    {
        FileCoverage[] files =
        [
            new(CoverageTestData.Lines1Of1CoveredWith0Of4Branches, $"{CoverageTestData.FilePath}-1"),
            new(CoverageTestData.Lines3Of3CoveredWith2Of2Branches, $"{CoverageTestData.FilePath}-2")
        ];

        Coverage coverage = new(files);

        double overallCoverage = coverage.CalculateOverallCoverage(CoverageType.Branch);

        Assert.That(overallCoverage, Is.EqualTo((double)2 / 6));
    }

    [Test]
    public void Coverage_CalculatePackageCoverage_LineCoverage_ReturnsCoverage()
    {
        FileCoverage[] files =
        [
            new(CoverageTestData.Lines3Of5Covered, $"{CoverageTestData.FilePath}-1", $"{CoverageTestData.PackageName}-1"),
            new(CoverageTestData.Lines0Of3Covered, $"{CoverageTestData.FilePath}-2", $"{CoverageTestData.PackageName}-1"),
            new(CoverageTestData.Lines4Of4Covered, $"{CoverageTestData.FilePath}-3", $"{CoverageTestData.PackageName}-2")
        ];

        Coverage coverage = new(files);

        double packageCoverage = coverage.CalculatePackageCoverage($"{CoverageTestData.PackageName}-1");

        Assert.That(packageCoverage, Is.EqualTo((double)3 / 8));
    }

    [Test]
    public void Coverage_CalculatePackageCoverage_BranchCoverage_ReturnsCoverage()
    {
        FileCoverage[] files =
        [
            new(CoverageTestData.Lines3Of3CoveredWith2Of2Branches, $"{CoverageTestData.FilePath}-1", $"{CoverageTestData.PackageName}-1"),
            new(CoverageTestData.Lines3Of5Covered, $"{CoverageTestData.FilePath}-2", $"{CoverageTestData.PackageName}-2"),
            new(CoverageTestData.Lines2Of3CoveredWith3Of4Branches, $"{CoverageTestData.FilePath}-3", $"{CoverageTestData.PackageName}-2")
        ];

        Coverage coverage = new(files);

        double packageCoverage = coverage.CalculatePackageCoverage($"{CoverageTestData.PackageName}-2", CoverageType.Branch);

        Assert.That(packageCoverage, Is.EqualTo((double)3 / 4));
    }

    [Test]
    public void Coverage_CalculatePackageCoverage_UnknownPackage_ThrowsException()
    {
        FileCoverage[] files =
        [
            new(CoverageTestData.Lines3Of5Covered, $"{CoverageTestData.FilePath}-1", $"{CoverageTestData.PackageName}-1"),
            new(CoverageTestData.LinesEmpty, $"{CoverageTestData.FilePath}-2", $"{CoverageTestData.PackageName}-2"),
            new(CoverageTestData.Lines0Of3Covered, $"{CoverageTestData.FilePath}-3", $"{CoverageTestData.PackageName}-2")
        ];

        Coverage coverage = new(files);

        Exception e = Assert.Throws<CoverageCalculationException>(() => coverage.CalculatePackageCoverage($"{CoverageTestData.PackageName}-unknown"));
        Assert.That(e.Message, Is.EqualTo("No files found for the specified package name"));
    }
}