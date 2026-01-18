using CoverageChecker.Results;
using CoverageChecker.Services;

namespace CoverageChecker.Tests.Unit.ServiceTests;

public class CoverageRegressionServiceTests
{
    private ICoverageRegressionService _service;

    [SetUp]
    public void Setup()
    {
        _service = new CoverageRegressionService();
    }

    [Test]
    public void CheckRegression_DeletedFile_ReturnsRegressionForAllTypes()
    {
        // Line coverage: 100%, Branch coverage: 100%
        FileCoverage baselineFile = CoverageTestData.CreateFile(CoverageTestData.Lines3Of3CoveredWith2Of2Branches, "Service.cs");
        Coverage baseline = new([baselineFile]);

        // Current has no files
        Coverage current = new([]);

        RegressionResult result = _service.CheckRegression(baseline, current);
        Assert.Multiple(() =>
        {
            Assert.That(result.HasRegressions, Is.True);
            // Regressions for both Line and Branch coverage
            Assert.That(result.RegressedFiles, Has.Count.EqualTo(2));
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.RegressedFiles.Any(r => r.CoverageType == CoverageType.Line), Is.True);
            Assert.That(result.RegressedFiles.Any(r => r.CoverageType == CoverageType.Branch), Is.True);
        });
    }

    [Test]
    public void CheckRegression_WorseLineCoverage_ReturnsLineRegression()
    {
        // Baseline: 100% (4/4) Line, NaN Branch (no branches)
        FileCoverage baselineFile = CoverageTestData.CreateFile(CoverageTestData.Lines4Of4Covered, "Service.cs");
        Coverage baseline = new([baselineFile]);

        // Current: 60% (3/5) Line
        FileCoverage currentFile = CoverageTestData.CreateFile(CoverageTestData.Lines3Of5Covered, "Service.cs");
        Coverage current = new([currentFile]);

        RegressionResult result = _service.CheckRegression(baseline, current);
        Assert.Multiple(() =>
        {
            Assert.That(result.HasRegressions, Is.True);
            Assert.That(result.RegressedFiles, Has.Count.EqualTo(1));
            Assert.That(result.RegressedFiles[0].CoverageType, Is.EqualTo(CoverageType.Line));
        });
    }

    [Test]
    public void CheckRegression_WorseBranchCoverage_ReturnsBranchRegression()
    {
        LineCoverage line = new(1, true, 4, 4); // 100% branch
        FileCoverage baselineFile = new("Service.cs");
        baselineFile.AddOrMergeLine(line, new CoverageMergeService());
        Coverage baseline = new([baselineFile]);

        LineCoverage line2 = new(1, true, 4, 2); // 50% branch
        FileCoverage currentFile = new FileCoverage("Service.cs");
        currentFile.AddOrMergeLine(line2, new CoverageMergeService());
        Coverage current = new([currentFile]);

        RegressionResult result = _service.CheckRegression(baseline, current);
        Assert.Multiple(() =>
        {
            Assert.That(result.HasRegressions, Is.True);
            // Line coverage is the same (100%), but branch regressed
            Assert.That(result.RegressedFiles, Has.Count.EqualTo(1));
        });
        Assert.That(result.RegressedFiles[0].CoverageType, Is.EqualTo(CoverageType.Branch));
    }

    [Test]
    public void CheckRegression_BetterCoverage_ReturnsNoRegression()
    {
        // Baseline: 60% (3/5)
        FileCoverage baselineFile = CoverageTestData.CreateFile(CoverageTestData.Lines3Of5Covered, "Service.cs");
        Coverage baseline = new([baselineFile]);

        // Current: 100% (4/4)
        FileCoverage currentFile = CoverageTestData.CreateFile(CoverageTestData.Lines4Of4Covered, "Service.cs");
        Coverage current = new([currentFile]);

        RegressionResult result = _service.CheckRegression(baseline, current);
        Assert.Multiple(() =>
        {
            Assert.That(result.HasRegressions, Is.False);
            Assert.That(result.RegressedFiles, Is.Empty);
        });
    }

    [Test]
    public void CheckRegression_MultipleFiles_IdentifiesRegressionsCorrectly()
    {
        FileCoverage baselineFile1 = CoverageTestData.CreateFile(CoverageTestData.Lines4Of4Covered, "File1.cs");
        FileCoverage baselineFile2 = CoverageTestData.CreateFile(CoverageTestData.Lines3Of5Covered, "File2.cs");
        Coverage baseline = new([baselineFile1, baselineFile2]);

        // File1: regressed line coverage
        FileCoverage currentFile1 = CoverageTestData.CreateFile(CoverageTestData.Lines3Of5Covered, "File1.cs");
        // File2: improved line coverage
        FileCoverage currentFile2 = CoverageTestData.CreateFile(CoverageTestData.Lines4Of4Covered, "File2.cs");
        Coverage current = new([currentFile1, currentFile2]);

        RegressionResult result = _service.CheckRegression(baseline, current);
        Assert.Multiple(() =>
        {
            Assert.That(result.HasRegressions, Is.True);
            Assert.That(result.RegressedFiles, Has.Count.EqualTo(1));
            Assert.That(result.RegressedFiles[0].Path, Is.EqualTo("File1.cs"));
            Assert.That(result.RegressedFiles[0].CoverageType, Is.EqualTo(CoverageType.Line));
        });
    }
}