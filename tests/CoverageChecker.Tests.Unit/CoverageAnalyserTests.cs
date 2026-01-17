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
    public void CoverageAnalyserConstructor_Matcher_WithLoggerFactory_InitializesCorrectly()
    {
        Matcher matcher = new();
        matcher.AddIncludePatterns(ValidGlobPatterns);
        Mock<Microsoft.Extensions.Logging.ILoggerFactory> mockLoggerFactory = new();
        
        CoverageAnalyser sut = new(ValidCoverageFormat, ValidDirectory, matcher, mockLoggerFactory.Object);

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

    [Test]
    public void AnalyseCoverage_ShouldHandleGitRepoRootFailure()
    {
        Mock<IFileFinder> mockFileFinder = new();
        Mock<IParserFactory> mockParserFactory = new();
        Mock<IGitService> mockGitService = new();
        Mock<ICoverageParser> mockParser = new();

        string filePath = "coverage.xml";
        mockFileFinder.Setup(f => f.FindFiles(ValidDirectory)).Returns([filePath]);

        mockGitService.Setup(g => g.GetRepoRoot()).Throws(new GitException("Not a git repo"));

        mockParserFactory.Setup(f => f.CreateParser(ValidCoverageFormat, It.IsAny<Coverage>(), It.IsAny<Microsoft.Extensions.Logging.ILoggerFactory>()))
            .Returns(mockParser.Object);

        CoverageAnalyser sut = new(ValidCoverageFormat, ValidDirectory, mockFileFinder.Object, mockParserFactory.Object, mockGitService.Object, Mock.Of<IDeltaCoverageService>());

        Assert.DoesNotThrow(() => sut.AnalyseCoverage());

        mockParser.Verify(p => p.ParseCoverage(filePath, null), Times.Once);
    }

    [Test]
    public void CoverageAnalyserConstructor_WithLoggerFactory_InitializesCorrectly()
    {
        Mock<Microsoft.Extensions.Logging.ILoggerFactory> mockLoggerFactory = new();
        CoverageAnalyser sut = new(ValidCoverageFormat, ValidDirectory, ValidGlobPatterns, mockLoggerFactory.Object);
        Assert.That(sut, Is.Not.Null);
    }

    [Test]
    public void AnalyseDeltaCoverage_ShouldAnalyseCoverage_WhenCoverageIsNull()
    {
        Mock<IFileFinder> mockFileFinder = new();
        Mock<IParserFactory> mockParserFactory = new();
        Mock<IGitService> mockGitService = new();
        Mock<IDeltaCoverageService> mockDeltaService = new();
        Mock<ICoverageParser> mockParser = new();

        string filePath = "coverage.xml";
        mockFileFinder.Setup(f => f.FindFiles(ValidDirectory)).Returns([filePath]);
        mockParserFactory.Setup(f => f.CreateParser(ValidCoverageFormat, It.IsAny<Coverage>(), It.IsAny<Microsoft.Extensions.Logging.ILoggerFactory>()))
            .Returns(mockParser.Object);

        CoverageAnalyser sut = new(ValidCoverageFormat, ValidDirectory, mockFileFinder.Object, mockParserFactory.Object, mockGitService.Object, mockDeltaService.Object);

        sut.AnalyseDeltaCoverage("main");

        mockFileFinder.Verify(f => f.FindFiles(ValidDirectory), Times.Once);
        mockDeltaService.Verify(s => s.FilterCoverage(It.IsAny<Coverage>(), It.IsAny<IDictionary<string, HashSet<int>>>()), Times.Once);
    }

    [Test]
    public void AnalyseCoverage_ShouldLogMessages_WhenLoggingIsEnabled()
    {
        Mock<Microsoft.Extensions.Logging.ILoggerFactory> mockLoggerFactory = new();
        Mock<Microsoft.Extensions.Logging.ILogger> mockLogger = new();
        Mock<IFileFinder> mockFileFinder = new();
        Mock<IParserFactory> mockParserFactory = new();
        Mock<IGitService> mockGitService = new();
        Mock<ICoverageParser> mockParser = new();

        mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
        mockLogger.Setup(l => l.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information)).Returns(true);

        mockFileFinder.Setup(f => f.FindFiles(ValidDirectory)).Returns(["file.xml"]);
        mockParserFactory.Setup(f => f.CreateParser(ValidCoverageFormat, It.IsAny<Coverage>(), It.IsAny<Microsoft.Extensions.Logging.ILoggerFactory>()))
            .Returns(mockParser.Object);

        CoverageAnalyser sut = new(ValidCoverageFormat, ValidDirectory, mockFileFinder.Object, mockParserFactory.Object, mockGitService.Object, Mock.Of<IDeltaCoverageService>(), mockLoggerFactory.Object);

        sut.AnalyseCoverage();

        mockLogger.Verify(l => l.Log(
            Microsoft.Extensions.Logging.LogLevel.Information,
            It.IsAny<Microsoft.Extensions.Logging.EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Test]
    public void AnalyseCoverage_ShouldThrowException_WhenNoFilesFound()
    {
        Mock<IFileFinder> mockFileFinder = new();
        Mock<IParserFactory> mockParserFactory = new();
        Mock<IGitService> mockGitService = new();

        mockFileFinder.Setup(f => f.FindFiles(ValidDirectory)).Returns([]);

        CoverageAnalyser sut = new(ValidCoverageFormat, ValidDirectory, mockFileFinder.Object, mockParserFactory.Object, mockGitService.Object, Mock.Of<IDeltaCoverageService>());

        Assert.Throws<NoCoverageFilesFoundException>(() => sut.AnalyseCoverage());
    }
}
