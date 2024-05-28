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

    [Test]
    public void FileCoverage_TryGetLine_LineExists_ReturnsTrue() {
        LineCoverage[] lines = [
            new LineCoverage(1, true, 1, 0),
            new LineCoverage(2, false, 6, 2),
            new LineCoverage(3, true, 4, 3)
        ];
        FileCoverage fileCoverage = new(lines, "coverage-file");

        bool result = fileCoverage.TryGetLine(1, out LineCoverage? line);

        Assert.Multiple(() => {
            Assert.That(result, Is.True);
            Assert.That(line, Is.EqualTo(lines[0]));
        });
    }

    [Test]
    public void FileCoverage_TryGetLine_LineDoesNotExist_ReturnsFalse() {
        LineCoverage[] lines = [
            new LineCoverage(1, true, 1, 0),
            new LineCoverage(2, false, 6, 2),
            new LineCoverage(3, true, 4, 3)
        ];
        FileCoverage fileCoverage = new(lines, "coverage-file");

        bool result = fileCoverage.TryGetLine(4, out LineCoverage? line);

        Assert.Multiple(() => {
            Assert.That(result, Is.False);
            Assert.That(line, Is.Null);
            Assert.That(fileCoverage.Path, Is.EqualTo("coverage-file"));
            Assert.That(fileCoverage.PackageName, Is.Null);
        });
    }

    [Test]
    public void FileCoverage_CalculateFileCoverage_Line_ReturnsCoverage() {
        FileCoverage fileCoverage = new([
            new LineCoverage(1, true),
            new LineCoverage(2, false),
            new LineCoverage(3, true)
        ], "coverage-file");

        double coverage = fileCoverage.CalculateFileCoverage();

        Assert.That(coverage, Is.EqualTo((double)2 / 3));
    }

    [Test]
    public void FileCoverage_CalculateFileCoverage_Branch_ReturnsCoverage() {
        FileCoverage fileCoverage = new([
            new LineCoverage(1, true, 1, 0),
            new LineCoverage(2, false, 6, 2),
            new LineCoverage(3, true, 4, 3)
        ], "coverage-file");

        double coverage = fileCoverage.CalculateFileCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo((double)5 / 11));
    }

    [Test]
    public void FileCoverage_CalculateClassCoverage_Line_ReturnsCoverage() {
        FileCoverage fileCoverage = new([
            new LineCoverage(1, true, className: "class-name-1"),
            new LineCoverage(2, false, className: "class-name-2"),
            new LineCoverage(3, true, className: "class-name-2")
        ], "coverage-file");

        double coverage = fileCoverage.CalculateClassCoverage("class-name-2");

        Assert.That(coverage, Is.EqualTo((double)1 / 2));
    }

    [Test]
    public void FileCoverage_CalculateClassCoverage_Branch_ReturnsCoverage() {
        FileCoverage fileCoverage = new([
            new LineCoverage(1, true, 1, 0, "class-name-1"),
            new LineCoverage(2, false, 6, 2, "class-name-2"),
            new LineCoverage(3, true, 4, 3, "class-name-2")
        ], "coverage-file");

        double coverage = fileCoverage.CalculateClassCoverage("class-name-1", CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo(0));
    }

    [Test]
    public void FileCoverage_CalculateClassCoverage_ClassDoesNotExist_ThrowsException() {
        FileCoverage fileCoverage = new([
            new LineCoverage(1, true, className: "class-name-1"),
            new LineCoverage(2, false, className: "class-name-2"),
            new LineCoverage(3, true, className: "class-name-2")
        ], "coverage-file");

        Assert.Throws<CoverageCalculationException>(() => fileCoverage.CalculateClassCoverage("class-name-3"));
    }

    [Test]
    public void FileCoverage_CalculateMethodCoverage_Line_ReturnsCoverage() {
        FileCoverage fileCoverage = new([
            new LineCoverage(1, true, methodName: "method-name-1"),
            new LineCoverage(2, false, methodName: "method-name-2"),
            new LineCoverage(3, true, methodName: "method-name-2")
        ], "coverage-file");

        double coverage = fileCoverage.CalculateMethodCoverage("method-name-2");

        Assert.That(coverage, Is.EqualTo((double)1 / 2));
    }

    [Test]
    public void FileCoverage_CalculateMethodCoverage_Branch_ReturnsCoverage() {
        FileCoverage fileCoverage = new([
            new LineCoverage(1, true, 1, 0, methodName: "method-name-1"),
            new LineCoverage(2, false, 6, 2, methodName: "method-name-2", methodSignature: "method-signature-1"),
            new LineCoverage(3, true, 4, 3, methodName: "method-name-2", methodSignature: "method-signature-2")
        ], "coverage-file");

        double coverage = fileCoverage.CalculateMethodCoverage("method-name-2", "method-signature-1", CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo((double)2 / 6));
    }

    [Test]
    public void FileCoverage_CalculateMethodCoverage_MethodDoesNotExist_ThrowsException() {
        FileCoverage fileCoverage = new([
            new LineCoverage(1, true, methodName: "method-name-1"),
            new LineCoverage(2, false, methodName: "method-name-2"),
            new LineCoverage(3, true, methodName: "method-name-2")
        ], "coverage-file");

        Assert.Throws<CoverageCalculationException>(() => fileCoverage.CalculateMethodCoverage("method-name-3"));
    }
}