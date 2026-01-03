using CoverageChecker.Results;
using CoverageChecker.Services;

namespace CoverageChecker.Tests.Unit.ResultTests;

public class FileCoverageTests
{
    [Test]
    public void FileCoverage_ConstructorWithLines_ReturnsObject()
    {
        LineCoverage[] lines =
        [
            new(1, true, 1, 0),
            new(2, false, 6, 2),
            new(3, true, 4, 3)
        ];
        FileCoverage fileCoverage = CoverageTestData.CreateFile(lines, CoverageTestData.FilePath);

        Assert.That(fileCoverage.Lines, Is.EqualTo(lines));
    }

    [Test]
    public void FileCoverage_ConstructorWithEmptyLines_ReturnsObject()
    {
        LineCoverage[] lines = [];
        FileCoverage fileCoverage = CoverageTestData.CreateFile(lines, CoverageTestData.FilePath, CoverageTestData.PackageName);

        Assert.Multiple(() =>
        {
            Assert.That(fileCoverage.Lines, Is.EqualTo(lines));
            Assert.That(fileCoverage.Path, Is.EqualTo(CoverageTestData.FilePath));
            Assert.That(fileCoverage.PackageName, Is.EqualTo(CoverageTestData.PackageName));
        });
    }

    [Test]
    public void AddOrMergeLine_LineExactlySame_DoesntUpdateLine()
    {
        FileCoverage fileCoverage = new(CoverageTestData.FilePath);
        CoverageMergeService service = new();
        LineCoverage line = new(1, true);

        fileCoverage.AddOrMergeLine(line, service);

        LineCoverage retrievedLine = fileCoverage.Lines.Single(l => l.LineNumber == 1);
        Assert.Multiple(() =>
        {
            Assert.That(retrievedLine.IsCovered, Is.True);
            Assert.That(retrievedLine.CoveredBranches, Is.Null);
            Assert.That(retrievedLine, Is.SameAs(line)); // Should be same object initially
        });

        // Add same line again
        fileCoverage.AddOrMergeLine(line, service);

        retrievedLine = fileCoverage.Lines.Single(l => l.LineNumber == 1);
        Assert.Multiple(() =>
        {
            Assert.That(retrievedLine.IsCovered, Is.True);
            Assert.That(retrievedLine.CoveredBranches, Is.Null);
            // Should still be the same line object as merge returns existing if equal
            Assert.That(retrievedLine, Is.SameAs(line));
        });
    }

    [Test]
    public void AddOrMergeLine_LineSubstantivelySame1_UpdatesLine()
    {
        FileCoverage fileCoverage = new(CoverageTestData.FilePath);
        CoverageMergeService service = new();

        fileCoverage.AddOrMergeLine(new LineCoverage(1, true, 1, 0), service);

        LineCoverage retrievedLine = fileCoverage.Lines.Single(l => l.LineNumber == 1);
        Assert.Multiple(() =>
        {
            Assert.That(retrievedLine.IsCovered, Is.True);
            Assert.That(retrievedLine.CoveredBranches, Is.EqualTo(0));
        });

        fileCoverage.AddOrMergeLine(new LineCoverage(1, true, 1, 1), service);

        retrievedLine = fileCoverage.Lines.Single(l => l.LineNumber == 1);
        Assert.Multiple(() =>
        {
            Assert.That(retrievedLine.IsCovered, Is.True);
            Assert.That(retrievedLine.CoveredBranches, Is.EqualTo(1));
        });

        fileCoverage.AddOrMergeLine(new LineCoverage(1, false, 1, 0), service);

        retrievedLine = fileCoverage.Lines.Single(l => l.LineNumber == 1);
        Assert.Multiple(() =>
        {
            Assert.That(retrievedLine.IsCovered, Is.True);
            Assert.That(retrievedLine.CoveredBranches, Is.EqualTo(1));
        });
    }

    [Test]
    public void AddOrMergeLine_LineSubstantivelySame2_UpdatesLine()
    {
        FileCoverage fileCoverage = new(CoverageTestData.FilePath);
        CoverageMergeService service = new();

        fileCoverage.AddOrMergeLine(new LineCoverage(2, false), service);

        LineCoverage retrievedLine = fileCoverage.Lines.Single(l => l.LineNumber == 2);
        Assert.Multiple(() =>
        {
            Assert.That(retrievedLine.IsCovered, Is.False);
            Assert.That(retrievedLine.CoveredBranches, Is.Null);
        });

        fileCoverage.AddOrMergeLine(new LineCoverage(2, true), service);

        retrievedLine = fileCoverage.Lines.Single(l => l.LineNumber == 2);
        Assert.Multiple(() =>
        {
            Assert.That(retrievedLine.IsCovered, Is.True);
            Assert.That(retrievedLine.CoveredBranches, Is.Null);
        });
    }

    [Test]
    public void AddOrMergeLine_LineNotSubstantivelySame_ThrowsException()
    {
        FileCoverage fileCoverage = new(CoverageTestData.FilePath);
        CoverageMergeService service = new();

        fileCoverage.AddOrMergeLine(new LineCoverage(1, true, 1, 0), service);

        Exception e = Assert.Throws<CoverageParseException>(() => fileCoverage.AddOrMergeLine(new LineCoverage(1, false, 2, 1), service));
        Assert.That(e.Message, Is.EqualTo("Cannot merge lines due to a branches mismatch"));
    }

    [Test]
    public void FileCoverage_CalculateFileCoverage_LineCoverage_ReturnsCoverage()
    {
        FileCoverage fileCoverage = CoverageTestData.CreateFile(CoverageTestData.Lines3Of5Covered, CoverageTestData.FilePath);

        double coverage = fileCoverage.CalculateFileCoverage();

        Assert.That(coverage, Is.EqualTo((double)3 / 5));
    }

    [Test]
    public void FileCoverage_CalculateFileCoverage_BranchCoverage_ReturnsCoverage()
    {
        FileCoverage fileCoverage = CoverageTestData.CreateFile(CoverageTestData.Lines2Of3CoveredWith3Of4Branches, CoverageTestData.FilePath);

        double coverage = fileCoverage.CalculateFileCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo((double)3 / 4));
    }

    [Test]
    public void FileCoverage_CalculateClassCoverage_LineCoverage_ReturnsCoverage()
    {
        FileCoverage fileCoverage = CoverageTestData.CreateFile([
            new LineCoverage(1, true, className: $"{CoverageTestData.ClassName}-1"),
            new LineCoverage(2, false, className: $"{CoverageTestData.ClassName}-2"),
            new LineCoverage(3, true, className: $"{CoverageTestData.ClassName}-2")
        ], CoverageTestData.FilePath);

        double coverage = fileCoverage.CalculateClassCoverage($"{CoverageTestData.ClassName}-2");

        Assert.That(coverage, Is.EqualTo((double)1 / 2));
    }

    [Test]
    public void FileCoverage_CalculateClassCoverage_BranchCoverage_ReturnsCoverage()
    {
        FileCoverage fileCoverage = CoverageTestData.CreateFile([
            new LineCoverage(1, true, 1, 0, $"{CoverageTestData.ClassName}-1"),
            new LineCoverage(2, false, 6, 2, $"{CoverageTestData.ClassName}-2"),
            new LineCoverage(3, true, 4, 3, $"{CoverageTestData.ClassName}-2")
        ], CoverageTestData.FilePath);

        double coverage = fileCoverage.CalculateClassCoverage($"{CoverageTestData.ClassName}-1", CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo(0));
    }

    [Test]
    public void FileCoverage_CalculateClassCoverage_UnknownClass_ThrowsException()
    {
        FileCoverage fileCoverage = CoverageTestData.CreateFile([
            new LineCoverage(1, true, className: $"{CoverageTestData.ClassName}-1"),
            new LineCoverage(2, false, className: $"{CoverageTestData.ClassName}-2"),
            new LineCoverage(3, true, className: $"{CoverageTestData.ClassName}-2")
        ], CoverageTestData.FilePath);

        Exception e = Assert.Throws<CoverageCalculationException>(() => fileCoverage.CalculateClassCoverage($"{CoverageTestData.ClassName}-unknown"));
        Assert.That(e.Message, Is.EqualTo("No lines found for the specified class name"));
    }

    [Test]
    public void FileCoverage_CalculateMethodCoverage_LineCoverage_ReturnsCoverage()
    {
        FileCoverage fileCoverage = CoverageTestData.CreateFile([
            new LineCoverage(1, true, methodName: $"{CoverageTestData.MethodName}-1"),
            new LineCoverage(2, false, methodName: $"{CoverageTestData.MethodName}-2"),
            new LineCoverage(3, true, methodName: $"{CoverageTestData.MethodName}-2")
        ], CoverageTestData.FilePath);

        double coverage = fileCoverage.CalculateMethodCoverage($"{CoverageTestData.MethodName}-2");

        Assert.That(coverage, Is.EqualTo((double)1 / 2));
    }

    [Test]
    public void FileCoverage_CalculateMethodCoverage_BranchCoverage_ReturnsCoverage()
    {
        FileCoverage fileCoverage = CoverageTestData.CreateFile([
            new LineCoverage(1, true, 1, 0, methodName: $"{CoverageTestData.MethodName}-1"),
            new LineCoverage(2, false, 6, 2, methodName: $"{CoverageTestData.MethodName}-2", methodSignature: $"{CoverageTestData.MethodSignature}-1"),
            new LineCoverage(3, true, 4, 3, methodName: $"{CoverageTestData.MethodName}-2", methodSignature: $"{CoverageTestData.MethodSignature}-2")
        ], CoverageTestData.FilePath);

        double coverage = fileCoverage.CalculateMethodCoverage($"{CoverageTestData.MethodName}-2", $"{CoverageTestData.MethodSignature}-1", CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo((double)2 / 6));
    }

    [Test]
    public void FileCoverage_CalculateMethodCoverage_UnknownMethod_ThrowsException()
    {
        FileCoverage fileCoverage = CoverageTestData.CreateFile([
            new LineCoverage(1, true, methodName: $"{CoverageTestData.MethodName}-1"),
            new LineCoverage(2, false, methodName: $"{CoverageTestData.MethodName}-2"),
            new LineCoverage(3, true, methodName: $"{CoverageTestData.MethodName}-2")
        ], CoverageTestData.FilePath);

        Exception e = Assert.Throws<CoverageCalculationException>(() => fileCoverage.CalculateMethodCoverage($"{CoverageTestData.MethodName}-unknown"));
        Assert.That(e.Message, Is.EqualTo("No lines found for the specified method"));
    }
}