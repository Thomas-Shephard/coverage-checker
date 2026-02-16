using CoverageChecker.Results;
using CoverageChecker.Services;

namespace CoverageChecker.Tests.Unit.ServiceTests;

public class CoverageRegressionServiceTests
{
    private CoverageRegressionService _service;

    [SetUp]
    public void Setup()
    {
        _service = new CoverageRegressionService();
    }

    [Test]
    public void CheckRegressionWithSplitFileAggregatesCoverageCorrectly()
    {
        // Baseline: File is in one package with 100% coverage (4/4 lines)
        FileCoverage baselineFile = CoverageTestData.CreateFile(CoverageTestData.Lines4Of4Covered, "SplitFile.cs", "OriginalPackage");
        Coverage baseline = new([baselineFile]);

        // Current: File is split across two packages, each containing different covered lines.
        // Package1: Line 1, 2 covered.
        // Package2: Line 3, 4 covered.
        // Combined they should represent 100% coverage for SplitFile.cs.
        FileCoverage currentFilePart1 = new("SplitFile.cs", "Package1");
        currentFilePart1.AddOrMergeLine(new LineCoverage(1, true), new CoverageMergeService());
        currentFilePart1.AddOrMergeLine(new LineCoverage(2, true), new CoverageMergeService());

        FileCoverage currentFilePart2 = new("SplitFile.cs", "Package2");
        currentFilePart2.AddOrMergeLine(new LineCoverage(3, true), new CoverageMergeService());
        currentFilePart2.AddOrMergeLine(new LineCoverage(4, true), new CoverageMergeService());

        Coverage current = new([currentFilePart1, currentFilePart2]);

        RegressionResult result = _service.CheckRegression(baseline, current);

        // Should NOT have regressions because the aggregated file has all lines covered.
        Assert.That(result.HasRegressions, Is.False);
    }

    [Test]
    public void CheckRegressionWithSplitBaselineAggregatesCorrectly()
    {
        // Baseline: File is split across two packages.
        // Package1: Line 1, 2 covered.
        // Package2: Line 3, 4 covered.
        // Total: 4/4 = 100%
        FileCoverage baselinePart1 = new("SplitFile.cs", "Package1");
        baselinePart1.AddOrMergeLine(new LineCoverage(1, true), new CoverageMergeService());
        baselinePart1.AddOrMergeLine(new LineCoverage(2, true), new CoverageMergeService());

        FileCoverage baselinePart2 = new("SplitFile.cs", "Package2");
        baselinePart2.AddOrMergeLine(new LineCoverage(3, true), new CoverageMergeService());
        baselinePart2.AddOrMergeLine(new LineCoverage(4, true), new CoverageMergeService());

        Coverage baseline = new([baselinePart1, baselinePart2]);

        // Current: Only 3 out of 4 lines covered.
        // Total: 3/4 = 75%
        FileCoverage currentFile = new("SplitFile.cs", "Package1");
        currentFile.AddOrMergeLine(new LineCoverage(1, true), new CoverageMergeService());
        currentFile.AddOrMergeLine(new LineCoverage(2, true), new CoverageMergeService());
        currentFile.AddOrMergeLine(new LineCoverage(3, true), new CoverageMergeService());
        currentFile.AddOrMergeLine(new LineCoverage(4, false), new CoverageMergeService());

        Coverage current = new([currentFile]);

        RegressionResult result = _service.CheckRegression(baseline, current);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasRegressions, Is.True);
            Assert.That(result.RegressedFiles, Has.Count.EqualTo(1));
            Assert.That(result.RegressedFiles[0].BaselineCoverage, Is.EqualTo(1.0));
            Assert.That(result.RegressedFiles[0].NewCoverage, Is.EqualTo(0.75));
        });
    }

    [Test]
    public void CheckRegressionWithSplitFileInDifferentPackagesNullifiesPackageNameOnRegression()
    {
        // Baseline: 100% coverage
        FileCoverage baselineFile = CoverageTestData.CreateFile(CoverageTestData.Lines4Of4Covered, "SplitFile.cs", "OriginalPackage");
        Coverage baseline = new([baselineFile]);

        // Current: Regressed coverage (3/4 lines) split across two DIFFERENT packages.
        FileCoverage currentFilePart1 = new("SplitFile.cs", "Package1");
        currentFilePart1.AddOrMergeLine(new LineCoverage(1, true), new CoverageMergeService());
        currentFilePart1.AddOrMergeLine(new LineCoverage(2, true), new CoverageMergeService());

        FileCoverage currentFilePart2 = new("SplitFile.cs", "Package2");
        currentFilePart2.AddOrMergeLine(new LineCoverage(3, true), new CoverageMergeService());
        currentFilePart2.AddOrMergeLine(new LineCoverage(4, false), new CoverageMergeService());

        Coverage current = new([currentFilePart1, currentFilePart2]);

        RegressionResult result = _service.CheckRegression(baseline, current);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasRegressions, Is.True);
            Assert.That(result.RegressedFiles, Has.Count.EqualTo(1));
            // PackageName should be null because it's ambiguous (Package1 and Package2)
            Assert.That(result.RegressedFiles[0].PackageName, Is.Null);
        });
    }

    [Test]
    public void CheckRegressionDeletedFileReturnsNoRegression()
    {
        // Line coverage: 100%, Branch coverage: 100%
        FileCoverage baselineFile = CoverageTestData.CreateFile(CoverageTestData.Lines3Of3CoveredWith2Of2Branches, "Service.cs");
        Coverage baseline = new([baselineFile]);

        // Current has no files
        Coverage current = new([]);

        RegressionResult result = _service.CheckRegression(baseline, current);
        Assert.Multiple(() =>
        {
            Assert.That(result.HasRegressions, Is.False);
            Assert.That(result.RegressedFiles, Is.Empty);
        });
    }

    [Test]
    public void CheckRegressionWorseLineCoverageReturnsLineRegression()
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
    public void CheckRegressionWorseBranchCoverageReturnsBranchRegression()
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
    public void CheckRegressionBetterCoverageReturnsNoRegression()
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
    public void CheckRegressionMultipleFilesIdentifiesRegressionsCorrectly()
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

    [Test]
    public void CheckRegressionHandlesNaNCurrentCoverage()
    {
        // Baseline has line coverage
        FileCoverage baselineFile = CoverageTestData.CreateFile(CoverageTestData.Lines4Of4Covered, "Service.cs");
        Coverage baseline = new([baselineFile]);

        // Current file exists but has NO lines (results in NaN line coverage)
        FileCoverage currentFile = new("Service.cs");
        Coverage current = new([currentFile]);

        RegressionResult result = _service.CheckRegression(baseline, current);

        Assert.That(result.HasRegressions, Is.False);
    }

    [Test]
    public void CheckRegressionWithEmptyBaselineAndCurrentReturnsNoRegression()
    {
        Coverage baseline = new([]);
        Coverage current = new([]);

        RegressionResult result = _service.CheckRegression(baseline, current);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasRegressions, Is.False);
            Assert.That(result.RegressedFiles, Is.Empty);
        });
    }

    [Test]
    public void CheckRegressionWithNewFileInCurrentOnlyReturnsNoRegression()
    {
        Coverage baseline = new([]);

        FileCoverage currentFile = CoverageTestData.CreateFile(CoverageTestData.Lines4Of4Covered, "NewFile.cs");
        Coverage current = new([currentFile]);

        RegressionResult result = _service.CheckRegression(baseline, current);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasRegressions, Is.False);
            Assert.That(result.RegressedFiles, Is.Empty);
        });
    }

    [Test]
    public void CheckRegressionWithRenamesIdentifiesRenamesCorrectly()
    {
        // Baseline: 100% (4/4)
        FileCoverage baselineFile = CoverageTestData.CreateFile(CoverageTestData.Lines4Of4Covered, "OldPath.cs");
        Coverage baseline = new([baselineFile]);

        // Current: 60% (3/5) but at a NEW path
        FileCoverage currentFile = CoverageTestData.CreateFile(CoverageTestData.Lines3Of5Covered, "NewPath.cs");
        Coverage current = new([currentFile]);

        // Rename mapping
        Dictionary<string, string> renames = new() { { "OldPath.cs", "NewPath.cs" } };

        RegressionResult result = _service.CheckRegression(baseline, current, renames);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasRegressions, Is.True);
            Assert.That(result.RegressedFiles, Has.Count.EqualTo(1));
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.RegressedFiles[0].Path, Is.EqualTo("NewPath.cs"));
            Assert.That(result.RegressedFiles[0].NewCoverage, Is.EqualTo(0.6));
        });
    }

    [Test]
    public void CheckRegressionWithRenamesAndPackageChangeIdentifiesRenamesCorrectly()
    {
        // Baseline: 100% (4/4) in "OldPackage"
        FileCoverage baselineFile = CoverageTestData.CreateFile(CoverageTestData.Lines4Of4Covered, "OldPath.cs", "OldPackage");
        Coverage baseline = new([baselineFile]);

        // Current: 60% (3/5) in "NewPackage" at a NEW path
        FileCoverage currentFile = CoverageTestData.CreateFile(CoverageTestData.Lines3Of5Covered, "NewPath.cs", "NewPackage");
        Coverage current = new([currentFile]);

        // Rename mapping
        Dictionary<string, string> renames = new() { { "OldPath.cs", "NewPath.cs" } };

        RegressionResult result = _service.CheckRegression(baseline, current, renames);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasRegressions, Is.True);
            Assert.That(result.RegressedFiles, Has.Count.EqualTo(1));
            Assert.That(result.RegressedFiles[0].Path, Is.EqualTo("NewPath.cs"));
            Assert.That(result.RegressedFiles[0].NewCoverage, Is.EqualTo(0.6));
        });
    }
}
