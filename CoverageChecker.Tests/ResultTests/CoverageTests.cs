using CoverageChecker.Results;

namespace CoverageChecker.Tests.ResultTests;

public class CoverageTests {
    [Test]
    public void Coverage_Valid_ReturnsObject() {
        FileCoverage[] files = [
            new FileCoverage([
                new LineCoverage(1, true, 1, 0),
                new LineCoverage(2, false, 8, 1),
                new LineCoverage(3, true, 2, 1)
            ], "coverage-file-1"),
            new FileCoverage([
                new LineCoverage(1, true, 1, 0),
                new LineCoverage(2, true, 3, 2),
                new LineCoverage(3, false, 4, 3)
            ], "coverage-file-2")
        ];
        Coverage coverage = new(files);

        Assert.That(coverage.Files, Is.EqualTo(files));
    }

    [Test]
    public void Coverage_ValidNoFiles_ReturnsObject() {
        FileCoverage[] files = [];
        Coverage coverage = new(files);

        Assert.That(coverage.Files, Is.EqualTo(files));
    }

    [Test]
    public void Coverage_TryGetFile_FileExists_ReturnsTrue() {
        FileCoverage[] files = [
            new FileCoverage([
                new LineCoverage(1, true, 1, 0),
                new LineCoverage(2, false, 8, 1),
                new LineCoverage(3, true, 2, 1)
            ], "coverage-file-1"),
            new FileCoverage([
                new LineCoverage(1, true, 1, 0),
                new LineCoverage(2, true, 3, 2),
                new LineCoverage(3, false, 4, 3)
            ], "coverage-file-2")
        ];
        Coverage coverage = new(files);

        FileCoverage? file = coverage.GetFile("coverage-file-1");

        Assert.That(file, Is.EqualTo(files[0]));
    }

    [Test]
    public void Coverage_TryGetFile_FileDoesNotExist_ReturnsFalse() {
        FileCoverage[] files = [
            new FileCoverage([
                new LineCoverage(1, true, 1, 0),
                new LineCoverage(2, false, 8, 1),
                new LineCoverage(3, true, 2, 1)
            ], "coverage-file-1"),
            new FileCoverage([
                new LineCoverage(1, true, 1, 0),
                new LineCoverage(2, true, 3, 2),
                new LineCoverage(3, false, 4, 3)
            ], "coverage-file-2")
        ];
        Coverage coverage = new(files);

        FileCoverage? file = coverage.GetFile("coverage-file-3");

        Assert.That(file, Is.Null);
    }

    [Test]
    public void Coverage_CalculateOverallCoverage_Line_ReturnsCoverage() {
        FileCoverage[] files = [
            new FileCoverage([
                new LineCoverage(1, true, 1, 0),
                new LineCoverage(2, false, 8, 1),
                new LineCoverage(3, true, 2, 1)
            ], "coverage-file-1"),
            new FileCoverage([
                new LineCoverage(1, true, 1, 0),
                new LineCoverage(2, true, 3, 2),
                new LineCoverage(3, false, 4, 3)
            ], "coverage-file-2")
        ];
        Coverage coverage = new(files);

        double overallCoverage = coverage.CalculateOverallCoverage();

        Assert.That(overallCoverage, Is.EqualTo((double)4 / 6));
    }

    [Test]
    public void Coverage_CalculateOverallCoverage_Branch_ReturnsCoverage() {
        FileCoverage[] files = [
            new FileCoverage([
                new LineCoverage(1, true, 1, 0),
                new LineCoverage(2, false, 8, 1),
                new LineCoverage(3, true, 2, 1)
            ], "coverage-file-1"),
            new FileCoverage([
                new LineCoverage(1, true, 1, 0),
                new LineCoverage(2, true, 3, 2),
                new LineCoverage(3, false, 4, 3)
            ], "coverage-file-2")
        ];
        Coverage coverage = new(files);

        double overallCoverage = coverage.CalculateOverallCoverage(CoverageType.Branch);

        Assert.That(overallCoverage, Is.EqualTo((double)7 / 19));
    }

    [Test]
    public void Coverage_CalculatePackageCoverage_Line_ReturnsCoverage() {
        FileCoverage[] files = [
            new FileCoverage([
                new LineCoverage(1, true, 1, 0),
                new LineCoverage(2, false, 8, 1),
                new LineCoverage(3, true, 2, 1)
            ], "coverage-file-1", "coverage-package-1"),
            new FileCoverage([
                new LineCoverage(1, true, 1, 0),
                new LineCoverage(2, true, 3, 2),
                new LineCoverage(3, false, 4, 3)
            ], "coverage-file-2", "coverage-package-1"),
            new FileCoverage([
                new LineCoverage(1, true, 1, 0),
                new LineCoverage(2, true, 3, 2),
                new LineCoverage(3, false, 4, 3)
            ], "coverage-file-3", "coverage-package-2")
        ];
        Coverage coverage = new(files);

        double packageCoverage = coverage.CalculatePackageCoverage("coverage-package-1");

        Assert.That(packageCoverage, Is.EqualTo((double)4 / 6));
    }

    [Test]
    public void Coverage_CalculatePackageCoverage_Branch_ReturnsCoverage() {
        FileCoverage[] files = [
            new FileCoverage([
                new LineCoverage(1, true, 1, 0),
                new LineCoverage(2, false, 8, 1),
                new LineCoverage(3, true, 2, 1)
            ], "coverage-file-1", "coverage-package-1"),
            new FileCoverage([
                new LineCoverage(1, true, 1, 0),
                new LineCoverage(2, true, 3, 2),
                new LineCoverage(3, false, 4, 3)
            ], "coverage-file-2", "coverage-package-2"),
            new FileCoverage([
                new LineCoverage(1, true, 1, 0),
                new LineCoverage(2, true, 3, 2),
                new LineCoverage(3, false, 4, 3)
            ], "coverage-file-3", "coverage-package-2")
        ];
        Coverage coverage = new(files);

        double packageCoverage = coverage.CalculatePackageCoverage("coverage-package-2", CoverageType.Branch);

        Assert.That(packageCoverage, Is.EqualTo((double)10 / 16));
    }

    [Test]
    public void Coverage_CalculatePackageCoverage_PackageDoesNotExit_ThrowsException() {
        FileCoverage[] files = [
            new FileCoverage([
                new LineCoverage(1, true, 1, 0),
                new LineCoverage(2, false, 8, 1),
                new LineCoverage(3, true, 2, 1)
            ], "coverage-file-1", "coverage-package-1"),
            new FileCoverage([
                new LineCoverage(1, true, 1, 0),
                new LineCoverage(2, true, 3, 2),
                new LineCoverage(3, false, 4, 3)
            ], "coverage-file-2", "coverage-package-2"),
            new FileCoverage([
                new LineCoverage(1, true, 1, 0),
                new LineCoverage(2, true, 3, 2),
                new LineCoverage(3, false, 4, 3)
            ], "coverage-file-3", "coverage-package-2")
        ];
        Coverage coverage = new(files);

        Assert.Throws<CoverageCalculationException>(() => coverage.CalculatePackageCoverage("coverage-package-3"));
    }
}