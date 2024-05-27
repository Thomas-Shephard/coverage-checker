using CoverageChecker.Results;

namespace CoverageChecker.Tests.ResultTests;

public class FileCoverageTests {
    [Test]
    public void FileCoverage_Valid_ReturnsObject() {
        LineCoverage[] lines = [
            new LineCoverage(1, true, 1, 0),
            new LineCoverage(2, false, 6, 2),
            new LineCoverage(3, true, 4, 3)
        ];
        FileCoverage fileCoverage = new(lines, "coverage-file");

        Assert.That(fileCoverage.Lines, Is.EqualTo(lines));
    }

    [Test]
    public void FileCoverage_ValidNoLines_ReturnsObject() {
        LineCoverage[] lines = [];
        FileCoverage fileCoverage = new(lines, "coverage-file", "package-name");

        Assert.Multiple(() => {
            Assert.That(fileCoverage.Lines, Is.EqualTo(lines));
            Assert.That(fileCoverage.Path, Is.EqualTo("coverage-file"));
            Assert.That(fileCoverage.PackageName, Is.EqualTo("package-name"));
        });
    }
}