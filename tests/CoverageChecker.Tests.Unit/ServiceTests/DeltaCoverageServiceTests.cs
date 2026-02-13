using CoverageChecker.Results;
using CoverageChecker.Services;
using CoverageChecker.Utils;

namespace CoverageChecker.Tests.Unit.ServiceTests;

public class DeltaCoverageServiceTests
{
    private DeltaCoverageService _sut;
    private readonly ICoverageMergeService _mergeService = new CoverageMergeService();

    [SetUp]
    public void Setup()
    {
        _sut = new DeltaCoverageService(_mergeService);
    }

    [Test]
    public void FilterCoverageShouldReturnEmptyCoverageWhenNoFilesMatch()
    {
        Coverage coverage = new();
        FileCoverage file = coverage.GetOrCreateFile("file1.cs");
        file.AddOrMergeLine(new LineCoverage(1, true), _mergeService);

        Dictionary<string, HashSet<int>> changedLines = new()
        {
            { "file2.cs", [1] }
        };

        Coverage result = _sut.FilterCoverage(coverage, changedLines).Coverage;

        Assert.That(result.Files, Is.Empty);
    }

    [Test]
    public void FilterCoverageShouldFilterLinesWhenFileMatches()
    {
        Coverage coverage = new();
        FileCoverage file = coverage.GetOrCreateFile("file1.cs");
        file.AddOrMergeLine(new LineCoverage(1, true), _mergeService);
        file.AddOrMergeLine(new LineCoverage(2, false), _mergeService);

        Dictionary<string, HashSet<int>> changedLines = new()
        {
            { "file1.cs", [1] }
        };

        Coverage result = _sut.FilterCoverage(coverage, changedLines).Coverage;

        Assert.That(result.Files, Has.Count.EqualTo(1));
        Assert.That(result.Files[0].Lines, Has.Count.EqualTo(1));
        Assert.That(result.Files[0].Lines[0].LineNumber, Is.EqualTo(1));
    }

    [Test]
    public void FilterCoverageShouldMatchPathWithDifferentSeparators()
    {
        Coverage coverage = new();
        string path = PathUtils.NormalizePath(@"folder\file1.cs");
        FileCoverage file = coverage.GetOrCreateFile(path);
        file.AddOrMergeLine(new LineCoverage(1, true), _mergeService);

        Dictionary<string, HashSet<int>> changedLines = new()
        {
            { PathUtils.NormalizePath("folder/file1.cs"), [1] }
        };

        Coverage result = _sut.FilterCoverage(coverage, changedLines).Coverage;

        Assert.That(result.Files, Has.Count.EqualTo(1));
    }

    [Test]
    public void FilterCoverageShouldMatchAbsoluteCoveragePathWithRelativeGitPath()
    {
        Coverage coverage = new();
        // The service now expects paths in 'changedLines' to be normalized absolute paths too, 
        // because GitService.GetChangedLines returns normalized absolute paths.
        string relativePath = "src/file1.cs";
        string absolutePath = PathUtils.GetNormalizedFullPath(relativePath);
        
        FileCoverage file = coverage.GetOrCreateFile(absolutePath);
        file.AddOrMergeLine(new LineCoverage(1, true), _mergeService);

        Dictionary<string, HashSet<int>> changedLines = new()
        {
            { absolutePath, [1] }
        };

        Coverage result = _sut.FilterCoverage(coverage, changedLines).Coverage;

        Assert.That(result.Files, Has.Count.EqualTo(1));
    }

    [Test]
    public void FilterCoverageShouldSetHasChangedLinesToTrueWhenLinesAreMatched()
    {
        Coverage coverage = new();
        FileCoverage file = coverage.GetOrCreateFile("file1.cs");
        file.AddOrMergeLine(new LineCoverage(1, true), _mergeService);

        Dictionary<string, HashSet<int>> changedLines = new()
        {
            { "file1.cs", [1] }
        };

        DeltaResult result = _sut.FilterCoverage(coverage, changedLines);

        Assert.That(result.HasChangedLines, Is.True);
    }

    [Test]
    public void FilterCoverageShouldSetHasChangedLinesToFalseWhenNoLinesAreMatched()
    {
        Coverage coverage = new();
        FileCoverage file = coverage.GetOrCreateFile("file1.cs");
        file.AddOrMergeLine(new LineCoverage(1, true), _mergeService);

        Dictionary<string, HashSet<int>> changedLines = new()
        {
            { "file2.cs", [1] }
        };

        DeltaResult result = _sut.FilterCoverage(coverage, changedLines);

        Assert.That(result.HasChangedLines, Is.False);
    }

    [Test]
    public void FilterCoverageShouldMergeFilesWhenFileAppearsInMultiplePackages()
    {
        Coverage coverage = new();
        string path = "file1.cs";

        FileCoverage file1 = coverage.GetOrCreateFile(path, "Package1");
        file1.AddOrMergeLine(new LineCoverage(1, true), _mergeService);

        FileCoverage file2 = coverage.GetOrCreateFile(path, "Package2");
        file2.AddOrMergeLine(new LineCoverage(1, true), _mergeService);
        file2.AddOrMergeLine(new LineCoverage(2, false), _mergeService);

        Dictionary<string, HashSet<int>> changedLines = new()
        {
            { path, [1, 2] }
        };

        Coverage result = _sut.FilterCoverage(coverage, changedLines).Coverage;

        Assert.That(result.Files, Has.Count.EqualTo(1));
        Assert.That(result.Files[0].Lines, Has.Count.EqualTo(2));
    }
}