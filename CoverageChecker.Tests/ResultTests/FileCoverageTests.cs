using CoverageChecker.Results;

namespace CoverageChecker.Tests.ResultTests;

public class FileCoverageTests {
    [Test]
    public void FileCoverage_Valid_ReturnsObject() {
        LineCoverage[] lines = [
            new LineCoverage(true, 1, 0),
            new LineCoverage(false, 6, 2),
            new LineCoverage(true, 4, 3)
        ];
        FileCoverage fileCoverage = new(lines);

        Assert.That(fileCoverage.Lines, Is.EqualTo(lines));
    }

    [Test]
    public void FileCoverage_ValidNoLines_ReturnsObject() {
        LineCoverage[] lines = [];
        FileCoverage fileCoverage = new(lines);

        Assert.That(fileCoverage.Lines, Is.EqualTo(lines));
    }
}