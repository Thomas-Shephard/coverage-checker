using CoverageChecker.Results;

namespace CoverageChecker.Tests.ResultTests;

public class CoverageTests {
    [Test]
    public void Coverage_Valid_ReturnsObject() {
        FileCoverage[] files = [
            new FileCoverage([
                new LineCoverage(true, 1, 0),
                new LineCoverage(false, 8, 1),
                new LineCoverage(true, 2, 1)
            ]),
            new FileCoverage([
                new LineCoverage(true, 1, 0),
                new LineCoverage(true, 3, 2),
                new LineCoverage(false, 4, 3)
            ])
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