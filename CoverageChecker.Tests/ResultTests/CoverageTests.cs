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
}