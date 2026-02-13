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
    public void CoverageAnalyserConstructorSingleGlobPatternInitializesCorrectly()
    {
        CoverageAnalyser sut = new(ValidCoverageFormat, ValidDirectory, ValidGlobPatterns[0]);

        Assert.That(sut, Is.Not.Null);
    }

    [Test]
    public void CoverageAnalyserConstructorMultipleGlobPatternsInitializesCorrectly()
    {
        CoverageAnalyser sut = new(ValidCoverageFormat, ValidDirectory, ValidGlobPatterns);

        Assert.That(sut, Is.Not.Null);
    }

    [Test]
    public void CoverageAnalyserConstructorMatcherInitializesCorrectly()
    {
        Matcher matcher = new();
        matcher.AddIncludePatterns(ValidGlobPatterns);
        CoverageAnalyser sut = new(ValidCoverageFormat, ValidDirectory, matcher);

        Assert.That(sut, Is.Not.Null);
    }

    [Test]
    public void CoverageAnalyserConstructorMatcherWithLoggerFactoryInitializesCorrectly()
    {
        Matcher matcher = new();
        matcher.AddIncludePatterns(ValidGlobPatterns);
        Mock<Microsoft.Extensions.Logging.ILoggerFactory> mockLoggerFactory = new();
        
        CoverageAnalyser sut = new(ValidCoverageFormat, ValidDirectory, matcher, mockLoggerFactory.Object);

        Assert.That(sut, Is.Not.Null);
    }

    [Test]
    public void CoverageAnalyserConstructorEmptyGlobPatternsThrowsException()
    {
        Exception e = Assert.Throws<ArgumentException>(() => _ = new CoverageAnalyser(ValidCoverageFormat, ValidDirectory, []));
        Assert.That(e.Message, Does.Contain("At least one glob pattern must be provided"));
    }

    [Test]
    public void AnalyseDeltaCoverageCallsServicesWithCorrectParameters()
    {
        Mock<IFileFinder> mockFileFinder = new();
        Mock<IParserFactory> mockParserFactory = new();
        Mock<IGitService> mockGitService = new();
        Mock<IDeltaCoverageService> mockDeltaService = new();
        Coverage coverage = new();
        Dictionary<string, HashSet<int>> changedLines = [];
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
    public void AnalyseCoverageShouldDetectFormatWhenAutoIsUsed()
    {
        Mock<IFileFinder> mockFileFinder = new();
        Mock<IParserFactory> mockParserFactory = new();
        Mock<IGitService> mockGitService = new();
        Mock<ICoverageParser> mockParser = new();

        string filePath = "coverage.xml";
        mockFileFinder.Setup(f => f.FindFiles(ValidDirectory)).Returns([filePath]);
        mockParserFactory.Setup(f => f.DetectFormat(filePath)).Returns(CoverageFormat.Cobertura);
        mockParserFactory.Setup(f => f.CreateParser(CoverageFormat.Cobertura, It.IsAny<Coverage>(), It.IsAny<Microsoft.Extensions.Logging.ILoggerFactory>()))
            .Returns(mockParser.Object);

        CoverageAnalyser sut = new(CoverageFormat.Auto, ValidDirectory, mockFileFinder.Object, mockParserFactory.Object, mockGitService.Object, Mock.Of<IDeltaCoverageService>());

        sut.AnalyseCoverage();

        mockParserFactory.Verify(f => f.DetectFormat(filePath), Times.Once);
        mockParserFactory.Verify(f => f.CreateParser(CoverageFormat.Cobertura, It.IsAny<Coverage>(), It.IsAny<Microsoft.Extensions.Logging.ILoggerFactory>()), Times.Once);
        mockParser.Verify(p => p.ParseCoverage(filePath, It.IsAny<string?>()), Times.Once);
    }

    [Test]
    public void AnalyseCoverageShouldCacheParsersForDifferentFormatsWhenAutoIsUsed()
    {
        Mock<IFileFinder> mockFileFinder = new();
        Mock<IParserFactory> mockParserFactory = new();
        Mock<IGitService> mockGitService = new();
        Mock<ICoverageParser> mockCoberturaParser = new();
        Mock<ICoverageParser> mockSonarQubeParser = new();

        string file1 = "coverage.cobertura.xml";
        string file2 = "coverage.sonarqube.xml";
        string file3 = "coverage.another.cobertura.xml";

        mockFileFinder.Setup(f => f.FindFiles(ValidDirectory)).Returns([file1, file2, file3]);
        
        mockParserFactory.Setup(f => f.DetectFormat(file1)).Returns(CoverageFormat.Cobertura);
        mockParserFactory.Setup(f => f.DetectFormat(file2)).Returns(CoverageFormat.SonarQube);
        mockParserFactory.Setup(f => f.DetectFormat(file3)).Returns(CoverageFormat.Cobertura);

        mockParserFactory.Setup(f => f.CreateParser(CoverageFormat.Cobertura, It.IsAny<Coverage>(), It.IsAny<Microsoft.Extensions.Logging.ILoggerFactory>()))
            .Returns(mockCoberturaParser.Object);
        mockParserFactory.Setup(f => f.CreateParser(CoverageFormat.SonarQube, It.IsAny<Coverage>(), It.IsAny<Microsoft.Extensions.Logging.ILoggerFactory>()))
            .Returns(mockSonarQubeParser.Object);

        CoverageAnalyser sut = new(CoverageFormat.Auto, ValidDirectory, mockFileFinder.Object, mockParserFactory.Object, mockGitService.Object, Mock.Of<IDeltaCoverageService>());

        sut.AnalyseCoverage();

        // Verify parsers were only created once per format
        mockParserFactory.Verify(f => f.CreateParser(CoverageFormat.Cobertura, It.IsAny<Coverage>(), It.IsAny<Microsoft.Extensions.Logging.ILoggerFactory>()), Times.Once);
        mockParserFactory.Verify(f => f.CreateParser(CoverageFormat.SonarQube, It.IsAny<Coverage>(), It.IsAny<Microsoft.Extensions.Logging.ILoggerFactory>()), Times.Once);

        // Verify correct parser was used for each file
        mockCoberturaParser.Verify(p => p.ParseCoverage(file1, It.IsAny<string?>()), Times.Once);
        mockSonarQubeParser.Verify(p => p.ParseCoverage(file2, It.IsAny<string?>()), Times.Once);
        mockCoberturaParser.Verify(p => p.ParseCoverage(file3, It.IsAny<string?>()), Times.Once);
    }

    [Test]
    public void AnalyseCoverageShouldHandleGitRepoRootFailure()
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
    public void CoverageAnalyserConstructorWithLoggerFactoryInitializesCorrectly()
    {
        Mock<Microsoft.Extensions.Logging.ILoggerFactory> mockLoggerFactory = new();
        CoverageAnalyser sut = new(ValidCoverageFormat, ValidDirectory, ValidGlobPatterns, mockLoggerFactory.Object);
        Assert.That(sut, Is.Not.Null);
    }

    [Test]
    public void AnalyseDeltaCoverageShouldAnalyseCoverageWhenCoverageIsNull()
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
    public void AnalyseCoverageShouldLogMessagesWhenLoggingIsEnabled()
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
    public void AnalyseCoverageShouldThrowExceptionWhenNoFilesFound()
    {
        Mock<IFileFinder> mockFileFinder = new();
        Mock<IParserFactory> mockParserFactory = new();
        Mock<IGitService> mockGitService = new();

        mockFileFinder.Setup(f => f.FindFiles(ValidDirectory)).Returns([]);

        CoverageAnalyser sut = new(ValidCoverageFormat, ValidDirectory, mockFileFinder.Object, mockParserFactory.Object, mockGitService.Object, Mock.Of<IDeltaCoverageService>());

        Assert.Throws<NoCoverageFilesFoundException>(() => sut.AnalyseCoverage());
    }
}
