using CoverageChecker.Parsers;
using CoverageChecker.Results;
using CoverageChecker.Services;
using Microsoft.Extensions.FileSystemGlobbing;
using Moq;

namespace CoverageChecker.Tests.Unit;

public class CoverageAnalyserTests
{
    private const CoverageFormat ValidCoverageFormat = CoverageFormat.Cobertura;
    private static readonly string ValidDirectory = Path.GetTempPath();
    private static readonly string[] ValidGlobPatterns = ["**/*.xml"];

    [Test]
    public void CoverageAnalyserConstructor_SingleGlobPattern_InitializesCorrectly()
    {
        CoverageAnalyser sut = new(ValidCoverageFormat, ValidDirectory, ValidGlobPatterns[0]);

        Assert.That(sut, Is.Not.Null);
    }

    [Test]
    public void CoverageAnalyserConstructor_MultipleGlobPatterns_InitializesCorrectly()
    {
        CoverageAnalyser sut = new(ValidCoverageFormat, ValidDirectory, ValidGlobPatterns);

        Assert.That(sut, Is.Not.Null);
    }

    [Test]
    public void CoverageAnalyserConstructor_Matcher_InitializesCorrectly()
    {
        Matcher matcher = new();
        matcher.AddIncludePatterns(ValidGlobPatterns);
        CoverageAnalyser sut = new(ValidCoverageFormat, ValidDirectory, matcher);

        Assert.That(sut, Is.Not.Null);
    }

    [Test]
    public void CoverageAnalyserConstructor_EmptyGlobPatterns_ThrowsException()
    {
        Exception e = Assert.Throws<ArgumentException>(() => _ = new CoverageAnalyser(ValidCoverageFormat, ValidDirectory, []));
        Assert.That(e.Message, Does.Contain("At least one glob pattern must be provided"));
    }

    [Test]
    public void AnalyseDeltaCoverage_CallsServicesWithCorrectParameters()
    {
        Mock<IFileFinder> mockFileFinder = new();
        Mock<IParserFactory> mockParserFactory = new();
        Mock<IGitService> mockGitService = new();
        Mock<IDeltaCoverageService> mockDeltaService = new();
        Coverage coverage = new();
        Dictionary<string, HashSet<int>> changedLines = new();
        DeltaResult deltaResult = new(new Coverage(), true);

        mockGitService.Setup(s => s.GetChangedLines("main", "HEAD")).Returns(changedLines);
        mockDeltaService.Setup(s => s.FilterCoverage(coverage, changedLines)).Returns(deltaResult);

        CoverageAnalyser sut = new(ValidCoverageFormat, ValidDirectory, mockFileFinder.Object, mockParserFactory.Object, mockGitService.Object, mockDeltaService.Object);

        DeltaResult result = sut.AnalyseDeltaCoverage("main", coverage);

        Assert.That(result, Is.EqualTo(deltaResult));
        mockGitService.Verify(s => s.GetChangedLines("main", "HEAD"), Times.Once);
        mockDeltaService.Verify(s => s.FilterCoverage(coverage, changedLines), Times.Once);
    }
}
