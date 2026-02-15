using CoverageChecker.Results;
using CoverageChecker.Services;

namespace CoverageChecker.Tests.Unit.ResultTests;

public class CoverageTests
{
    [Test]
    public void CoverageConstructorWithFilesReturnsObject()
    {
        FileCoverage[] files =
        [
            CoverageTestData.CreateFile(CoverageTestData.Lines3Of5Covered, $"{CoverageTestData.FilePath}-1"),
            CoverageTestData.CreateFile(CoverageTestData.Lines4Of4Covered, $"{CoverageTestData.FilePath}-2")
        ];

        Coverage coverage = new(files);

        Assert.Multiple(() =>
        {
            Assert.That(coverage.Files, Is.EqualTo(files));
            Assert.That(coverage.Lines, Is.EqualTo(new List<LineCoverage>([.. files[0].Lines, .. files[1].Lines])));
        });
    }

    [Test]
    public void CoverageConstructorWithEmptyFilesReturnsObject()
    {
        FileCoverage[] files = [];

        Coverage coverage = new(files);

        Assert.Multiple(() =>
        {
            Assert.That(coverage.Files, Is.Empty);
            Assert.That(coverage.Lines, Is.Empty);
        });
    }

    [Test]
    public void CoverageGetOrCreateFileReturnsFileIfExists()
    {
        Coverage coverage = new();
        CoverageMergeService service = new();

        FileCoverage coverageFile1 = coverage.GetOrCreateFile($"{CoverageTestData.FilePath}-1");

        coverageFile1.AddOrMergeLine(new LineCoverage(1, true, 1, 0), service);
        coverageFile1.AddOrMergeLine(new LineCoverage(2, false, 8, 1), service);
        coverageFile1.AddOrMergeLine(new LineCoverage(3, true, 2, 1), service);

        FileCoverage coverageFile2 = coverage.GetOrCreateFile($"{CoverageTestData.FilePath}-2");

        coverageFile2.AddOrMergeLine(new LineCoverage(1, true, 1, 0), service);
        coverageFile2.AddOrMergeLine(new LineCoverage(2, true, 3, 2), service);
        coverageFile2.AddOrMergeLine(new LineCoverage(3, false, 4, 3), service);

        Assert.Multiple(() =>
        {
            Assert.That(coverage.GetOrCreateFile($"{CoverageTestData.FilePath}-1"), Is.EqualTo(coverageFile1));
            Assert.That(coverage.GetOrCreateFile($"{CoverageTestData.FilePath}-2"), Is.EqualTo(coverageFile2));
            Assert.That(coverage.GetOrCreateFile($"{CoverageTestData.FilePath}-3").Lines, Is.Empty);
        });
    }

    [Test]
    public void CoverageCalculateOverallCoverageLineCoverageReturnsCoverage()
    {
        FileCoverage[] files =
        [
            CoverageTestData.CreateFile(CoverageTestData.Lines3Of5Covered, $"{CoverageTestData.FilePath}-1"),
            CoverageTestData.CreateFile(CoverageTestData.Lines4Of4Covered, $"{CoverageTestData.FilePath}-2")
        ];

        Coverage coverage = new(files);

        double overallCoverage = coverage.CalculateOverallCoverage();

        Assert.That(overallCoverage, Is.EqualTo((double)7 / 9));
    }

    [Test]
    public void CoverageCalculateOverallCoverageBranchCoverageReturnsCoverage()
    {
        FileCoverage[] files =
        [
            CoverageTestData.CreateFile(CoverageTestData.Lines1Of1CoveredWith0Of4Branches, $"{CoverageTestData.FilePath}-1"),
            CoverageTestData.CreateFile(CoverageTestData.Lines3Of3CoveredWith2Of2Branches, $"{CoverageTestData.FilePath}-2")
        ];

        Coverage coverage = new(files);

        double overallCoverage = coverage.CalculateOverallCoverage(CoverageType.Branch);

        Assert.That(overallCoverage, Is.EqualTo((double)2 / 6));
    }

    [Test]
    public void CoverageCalculatePackageCoverageLineCoverageReturnsCoverage()
    {
        FileCoverage[] files =
        [
            CoverageTestData.CreateFile(CoverageTestData.Lines3Of5Covered, $"{CoverageTestData.FilePath}-1", $"{CoverageTestData.PackageName}-1"),
            CoverageTestData.CreateFile(CoverageTestData.Lines0Of3Covered, $"{CoverageTestData.FilePath}-2", $"{CoverageTestData.PackageName}-1"),
            CoverageTestData.CreateFile(CoverageTestData.Lines4Of4Covered, $"{CoverageTestData.FilePath}-3", $"{CoverageTestData.PackageName}-2")
        ];

        Coverage coverage = new(files);

        double packageCoverage = coverage.CalculatePackageCoverage($"{CoverageTestData.PackageName}-1");

        Assert.That(packageCoverage, Is.EqualTo((double)3 / 8));
    }

    [Test]
    public void CoverageCalculatePackageCoverageBranchCoverageReturnsCoverage()
    {
        FileCoverage[] files =
        [
            CoverageTestData.CreateFile(CoverageTestData.Lines3Of3CoveredWith2Of2Branches, $"{CoverageTestData.FilePath}-1", $"{CoverageTestData.PackageName}-1"),
            CoverageTestData.CreateFile(CoverageTestData.Lines3Of5Covered, $"{CoverageTestData.FilePath}-2", $"{CoverageTestData.PackageName}-2"),
            CoverageTestData.CreateFile(CoverageTestData.Lines2Of3CoveredWith3Of4Branches, $"{CoverageTestData.FilePath}-3", $"{CoverageTestData.PackageName}-2")
        ];

        Coverage coverage = new(files);

        double packageCoverage = coverage.CalculatePackageCoverage($"{CoverageTestData.PackageName}-2", CoverageType.Branch);

        Assert.That(packageCoverage, Is.EqualTo((double)3 / 4));
    }

    [Test]
    public void CoverageCalculatePackageCoverageUnknownPackageThrowsException()
    {
        FileCoverage[] files =
        [
            CoverageTestData.CreateFile(CoverageTestData.Lines3Of5Covered, $"{CoverageTestData.FilePath}-1", $"{CoverageTestData.PackageName}-1"),
            CoverageTestData.CreateFile(CoverageTestData.LinesEmpty, $"{CoverageTestData.FilePath}-2", $"{CoverageTestData.PackageName}-2"),
            CoverageTestData.CreateFile(CoverageTestData.Lines0Of3Covered, $"{CoverageTestData.FilePath}-3", $"{CoverageTestData.PackageName}-2")
        ];

        Coverage coverage = new(files);

        Exception e = Assert.Throws<CoverageCalculationException>(() => coverage.CalculatePackageCoverage($"{CoverageTestData.PackageName}-unknown"));
        Assert.That(e.Message, Is.EqualTo("No files found for the specified package name"));
    }
}