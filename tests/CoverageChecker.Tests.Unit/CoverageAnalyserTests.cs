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

    private static CoverageAnalyserOptions CreateDefaultOptions() => new()
    {
        CoverageFormat = ValidCoverageFormat,
        Directory = ValidDirectory,
        GlobPatterns = ValidGlobPatterns
    };

    [Test]
    public void CoverageAnalyserConstructorOptionsInitializesCorrectly()
    {
        CoverageAnalyser sut = new(CreateDefaultOptions());

        Assert.That(sut, Is.Not.Null);
    }

    [Test]
    public void CoverageAnalyserConstructorMatcherInitializesCorrectly()
    {
        Matcher matcher = new();
        matcher.AddIncludePatterns(ValidGlobPatterns);
        CoverageAnalyser sut = new(CreateDefaultOptions(), matcher);

        Assert.That(sut, Is.Not.Null);
    }

    [Test]
    public void CoverageAnalyserConstructorMatcherWithLoggerFactoryInitializesCorrectly()
    {
        Matcher matcher = new();
        matcher.AddIncludePatterns(ValidGlobPatterns);
        Mock<Microsoft.Extensions.Logging.ILoggerFactory> mockLoggerFactory = new();
        
        CoverageAnalyser sut = new(CreateDefaultOptions(), matcher, mockLoggerFactory.Object);

        Assert.That(sut, Is.Not.Null);
    }

    [Test]
    public void CoverageAnalyserConstructorEmptyGlobPatternsThrowsException()
    {
        CoverageAnalyserOptions options = new() { GlobPatterns = [] };
        Exception e = Assert.Throws<ArgumentException>(() => _ = new CoverageAnalyser(options));
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

        CoverageAnalyser sut = new(CreateDefaultOptions(), mockFileFinder.Object, mockParserFactory.Object, mockGitService.Object, mockDeltaService.Object, Mock.Of<ICoverageRegressionService>());

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

        CoverageAnalyserOptions options = new() { CoverageFormat = CoverageFormat.Auto, Directory = ValidDirectory };
        CoverageAnalyser sut = new(options, mockFileFinder.Object, mockParserFactory.Object, mockGitService.Object, Mock.Of<IDeltaCoverageService>(), Mock.Of<ICoverageRegressionService>());

        sut.AnalyseCoverage();

        mockParserFactory.Verify(f => f.DetectFormat(filePath), Times.Once);
        mockParserFactory.Verify(f => f.CreateParser(CoverageFormat.Cobertura, It.IsAny<Coverage>(), It.IsAny<Microsoft.Extensions.Logging.ILoggerFactory>()), Times.Once);
        mockParser.Verify(p => p.ParseCoverage(filePath, It.IsAny<string?>()), Times.Once);
    }

    [Test]
    public void AnalyseCoverageShouldFilterFilesWhenIncludeOrExcludeIsUsed()
    {
        Mock<IFileFinder> mockFileFinder = new();
        Mock<IParserFactory> mockParserFactory = new();
        Mock<IGitService> mockGitService = new();
        Mock<ICoverageParser> mockParser = new();

        string currentDir = Environment.CurrentDirectory;
        string file1 = Path.Combine(currentDir, "src/File1.cs");
        string file2 = Path.Combine(currentDir, "tests/File2.cs");
        string filePath = "coverage.xml";
        mockFileFinder.Setup(f => f.FindFiles(ValidDirectory)).Returns([filePath]);
        mockParserFactory.Setup(f => f.DetectFormat(filePath)).Returns(CoverageFormat.Cobertura);

        mockParserFactory.Setup(f => f.CreateParser(CoverageFormat.Cobertura, It.IsAny<Coverage>(), It.IsAny<Microsoft.Extensions.Logging.ILoggerFactory>()))
                         .Callback<CoverageFormat, Coverage, Microsoft.Extensions.Logging.ILoggerFactory>((_, c, _) =>
                         {
                             c.GetOrCreateFile(file1);
                             c.GetOrCreateFile(file2);
                         })
                         .Returns(mockParser.Object);

        // Filter: include only src/**
        CoverageAnalyserOptions options = CreateDefaultOptions() with { CoverageFormat = CoverageFormat.Auto, Include = ["src/**"] };
        CoverageAnalyser sut = new(options, mockFileFinder.Object, mockParserFactory.Object, mockGitService.Object, Mock.Of<IDeltaCoverageService>(), Mock.Of<ICoverageRegressionService>());

        Coverage result = sut.AnalyseCoverage();

        Assert.That(result.Files, Has.Count.EqualTo(1));
        Assert.That(result.Files[0].Path, Is.EqualTo(file1));
    }

    [Test]
    public void AnalyseCoverageShouldFilterFilesWhenIncludeAndExcludeAreCombined()
    {
        Mock<IFileFinder> mockFileFinder = new();
        Mock<IParserFactory> mockParserFactory = new();
        Mock<IGitService> mockGitService = new();
        Mock<ICoverageParser> mockParser = new();

        string currentDir = Environment.CurrentDirectory;
        string file1 = Path.Combine(currentDir, "src/File1.cs");
        string file2 = Path.Combine(currentDir, "src/Internal/File2.cs");
        string filePath = "coverage.xml";
        mockFileFinder.Setup(f => f.FindFiles(ValidDirectory)).Returns([filePath]);
        mockParserFactory.Setup(f => f.DetectFormat(filePath)).Returns(CoverageFormat.Cobertura);

        mockParserFactory.Setup(f => f.CreateParser(CoverageFormat.Cobertura, It.IsAny<Coverage>(), It.IsAny<Microsoft.Extensions.Logging.ILoggerFactory>()))
                         .Callback<CoverageFormat, Coverage, Microsoft.Extensions.Logging.ILoggerFactory>((_, c, _) =>
                         {
                             c.GetOrCreateFile(file1);
                             c.GetOrCreateFile(file2);
                         })
                         .Returns(mockParser.Object);

        // Filter: include src/** but exclude src/Internal/**
        CoverageAnalyserOptions options = CreateDefaultOptions() with { CoverageFormat = CoverageFormat.Auto, Include = ["src/**"], Exclude = ["src/Internal/**"] };
        CoverageAnalyser sut = new(options, mockFileFinder.Object, mockParserFactory.Object, mockGitService.Object, Mock.Of<IDeltaCoverageService>(), Mock.Of<ICoverageRegressionService>());

        Coverage result = sut.AnalyseCoverage();

        Assert.That(result.Files, Has.Count.EqualTo(1));
        Assert.That(result.Files[0].Path, Is.EqualTo(file1));
    }

    [Test]
    public void AnalyseCoverageShouldSupportNegativePatternsInIncludeList()
    {
        Mock<IFileFinder> mockFileFinder = new();
        Mock<IParserFactory> mockParserFactory = new();
        Mock<IGitService> mockGitService = new();
        Mock<ICoverageParser> mockParser = new();

        string currentDir = Environment.CurrentDirectory;
        string file1 = Path.Combine(currentDir, "src/File1.cs");
        string file2 = Path.Combine(currentDir, "tests/File2.cs");
        string filePath = "coverage.xml";
        mockFileFinder.Setup(f => f.FindFiles(ValidDirectory)).Returns([filePath]);
        mockParserFactory.Setup(f => f.DetectFormat(filePath)).Returns(CoverageFormat.Cobertura);

        mockParserFactory.Setup(f => f.CreateParser(CoverageFormat.Cobertura, It.IsAny<Coverage>(), It.IsAny<Microsoft.Extensions.Logging.ILoggerFactory>()))
                         .Callback<CoverageFormat, Coverage, Microsoft.Extensions.Logging.ILoggerFactory>((_, c, _) =>
                         {
                             c.GetOrCreateFile(file1);
                             c.GetOrCreateFile(file2);
                         })
                         .Returns(mockParser.Object);

        // Filter: ONLY negative pattern in Include list
        CoverageAnalyserOptions options = CreateDefaultOptions() with { CoverageFormat = CoverageFormat.Auto, Include = ["!tests/**"] };
        CoverageAnalyser sut = new(options, mockFileFinder.Object, mockParserFactory.Object, mockGitService.Object, Mock.Of<IDeltaCoverageService>(), Mock.Of<ICoverageRegressionService>());

        Coverage result = sut.AnalyseCoverage();

        // Should include src/File1.cs and exclude tests/File2.cs
        Assert.That(result.Files, Has.Count.EqualTo(1));
        Assert.That(result.Files[0].Path, Is.EqualTo(file1));
    }

    [Test]
    public void AnalyseCoverageShouldExcludeFilesWhenExcludeHasLeadingExclamationMark()
    {
        Mock<IFileFinder> mockFileFinder = new();
        Mock<IParserFactory> mockParserFactory = new();
        Mock<IGitService> mockGitService = new();
        Mock<ICoverageParser> mockParser = new();

        string currentDir = Environment.CurrentDirectory;
        string file1 = Path.Combine(currentDir, "src/File1.cs");
        string file2 = Path.Combine(currentDir, "tests/File2.cs");
        string filePath = "coverage.xml";
        mockFileFinder.Setup(f => f.FindFiles(ValidDirectory)).Returns([filePath]);
        mockParserFactory.Setup(f => f.DetectFormat(filePath)).Returns(CoverageFormat.Cobertura);

        mockParserFactory.Setup(f => f.CreateParser(CoverageFormat.Cobertura, It.IsAny<Coverage>(), It.IsAny<Microsoft.Extensions.Logging.ILoggerFactory>()))
                         .Callback<CoverageFormat, Coverage, Microsoft.Extensions.Logging.ILoggerFactory>((_, c, _) =>
                         {
                             c.GetOrCreateFile(file1);
                             c.GetOrCreateFile(file2);
                         })
                         .Returns(mockParser.Object);

        // Filter: exclude tests/** using ! prefix in Exclude list (which should be handled correctly)
        CoverageAnalyserOptions options = CreateDefaultOptions() with { CoverageFormat = CoverageFormat.Auto, Exclude = ["!tests/**"] };
        CoverageAnalyser sut = new(options, mockFileFinder.Object, mockParserFactory.Object, mockGitService.Object, Mock.Of<IDeltaCoverageService>(), Mock.Of<ICoverageRegressionService>());

        Coverage result = sut.AnalyseCoverage();

        Assert.That(result.Files, Has.Count.EqualTo(1));
        Assert.That(result.Files[0].Path, Is.EqualTo(file1));
    }

    [Test]
    public void FilterFilesShouldUseCurrentDirectoryWhenGitRootIsNull()
    {
        Mock<IFileFinder> mockFileFinder = new();
        Mock<IParserFactory> mockParserFactory = new();
        Mock<IGitService> mockGitService = new();
        Mock<ICoverageParser> mockParser = new();

        string currentDir = Environment.CurrentDirectory;
        string file1 = Path.Combine(currentDir, "src/File1.cs");
        string coverageDir = Path.Combine(currentDir, "coverage_reports");
        
        mockFileFinder.Setup(f => f.FindFiles(coverageDir)).Returns(["coverage.xml"]);
        mockGitService.Setup(g => g.GetRepoRoot()).Throws(new GitException("Not in git"));
        mockParserFactory.Setup(f => f.DetectFormat(It.IsAny<string>())).Returns(CoverageFormat.Cobertura);

        mockParserFactory.Setup(f => f.CreateParser(It.IsAny<CoverageFormat>(), It.IsAny<Coverage>(), It.IsAny<Microsoft.Extensions.Logging.ILoggerFactory>()))
                         .Callback<CoverageFormat, Coverage, Microsoft.Extensions.Logging.ILoggerFactory>((_, c, _) =>
                         {
                             c.GetOrCreateFile(file1);
                         })
                         .Returns(mockParser.Object);

        // Filter: include src/** (relative to current directory, NOT coverageDir)
        CoverageAnalyserOptions options = CreateDefaultOptions() with 
        { 
            Directory = coverageDir,
            Include = ["src/**"] 
        };
        CoverageAnalyser sut = new(options, mockFileFinder.Object, mockParserFactory.Object, mockGitService.Object, Mock.Of<IDeltaCoverageService>(), Mock.Of<ICoverageRegressionService>());

        Coverage result = sut.AnalyseCoverage();

        Assert.That(result.Files, Has.Count.EqualTo(1));
        Assert.That(result.Files[0].Path, Is.EqualTo(file1));
    }

    [Test]
    public void FilterFilesShouldHandleDifferentDrivesGracefully()
    {
        Mock<IFileFinder> mockFileFinder = new();
        Mock<IParserFactory> mockParserFactory = new();
        Mock<IGitService> mockGitService = new();
        Mock<ICoverageParser> mockParser = new();

        // Determine a path that will definitely have a different root than Environment.CurrentDirectory
        // On Windows, use a different drive letter.
        // On Linux/Unix, use a relative path. Path.GetPathRoot will return "" while the root (CurrentDirectory) will be "/".
        string fileOnOtherDrive = Path.DirectorySeparatorChar == '\\'
            // ReSharper disable once NullableWarningSuppressionIsUsed
            ? (Path.GetPathRoot(Environment.CurrentDirectory)!.StartsWith("C", StringComparison.OrdinalIgnoreCase) ? "D:\\File.cs" : "C:\\File.cs")
            : "RelativeFile.cs";

        mockFileFinder.Setup(f => f.FindFiles(ValidDirectory)).Returns(["coverage.xml"]);
        mockParserFactory.Setup(f => f.DetectFormat(It.IsAny<string>())).Returns(CoverageFormat.Cobertura);

        mockParserFactory.Setup(f => f.CreateParser(It.IsAny<CoverageFormat>(), It.IsAny<Coverage>(), It.IsAny<Microsoft.Extensions.Logging.ILoggerFactory>()))
                         .Callback<CoverageFormat, Coverage, Microsoft.Extensions.Logging.ILoggerFactory>((_, c, _) =>
                         {
                             c.GetOrCreateFile(fileOnOtherDrive);
                         })
                         .Returns(mockParser.Object);

        // Include everything, but the different drive/root should still trigger exclusion
        CoverageAnalyserOptions options = CreateDefaultOptions() with { Include = ["**/*"] };
        CoverageAnalyser sut = new(options, mockFileFinder.Object, mockParserFactory.Object, mockGitService.Object, Mock.Of<IDeltaCoverageService>(), Mock.Of<ICoverageRegressionService>());

        Coverage result = sut.AnalyseCoverage();

        Assert.That(result.Files, Is.Empty);
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

        CoverageAnalyser sut = new(CreateDefaultOptions(), mockFileFinder.Object, mockParserFactory.Object, mockGitService.Object, Mock.Of<IDeltaCoverageService>(), Mock.Of<ICoverageRegressionService>());

        Assert.DoesNotThrow(() => sut.AnalyseCoverage());

        mockParser.Verify(p => p.ParseCoverage(filePath, null), Times.Once);
    }

    [Test]
    public void CoverageAnalyserConstructorWithLoggerFactoryInitializesCorrectly()
    {
        Mock<Microsoft.Extensions.Logging.ILoggerFactory> mockLoggerFactory = new();
        CoverageAnalyser sut = new(CreateDefaultOptions(), mockLoggerFactory.Object);
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

        CoverageAnalyser sut = new(CreateDefaultOptions(), mockFileFinder.Object, mockParserFactory.Object, mockGitService.Object, mockDeltaService.Object, Mock.Of<ICoverageRegressionService>());

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

        CoverageAnalyser sut = new(CreateDefaultOptions(), mockFileFinder.Object, mockParserFactory.Object, mockGitService.Object, Mock.Of<IDeltaCoverageService>(), Mock.Of<ICoverageRegressionService>(), mockLoggerFactory.Object);

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

        CoverageAnalyser sut = new(CreateDefaultOptions(), mockFileFinder.Object, mockParserFactory.Object, mockGitService.Object, Mock.Of<IDeltaCoverageService>(), Mock.Of<ICoverageRegressionService>());

        Assert.Throws<NoCoverageFilesFoundException>(() => sut.AnalyseCoverage());
    }

    [Test]
    public void AnalyseCoverageShouldUseRootDirectoryForFilteringWhenInGitRepo()
    {
        Mock<IFileFinder> mockFileFinder = new();
        Mock<IParserFactory> mockParserFactory = new();
        Mock<IGitService> mockGitService = new();
        Mock<ICoverageParser> mockParser = new();

        string rootDir = Path.Combine(ValidDirectory, "repo_root");
        string file1 = Path.Combine(rootDir, "src/File1.cs");
        mockFileFinder.Setup(f => f.FindFiles(ValidDirectory)).Returns(["coverage.xml"]);
        mockGitService.Setup(g => g.GetRepoRoot()).Returns(rootDir);

        mockParserFactory.Setup(f => f.CreateParser(It.IsAny<CoverageFormat>(), It.IsAny<Coverage>(), It.IsAny<Microsoft.Extensions.Logging.ILoggerFactory>()))
                         .Callback<CoverageFormat, Coverage, Microsoft.Extensions.Logging.ILoggerFactory>((_, c, _) =>
                         {
                             c.GetOrCreateFile(file1);
                         })
                         .Returns(mockParser.Object);

        // Filter: include only src/** (relative to repo_root)
        CoverageAnalyserOptions options = CreateDefaultOptions() with { Include = ["src/**"] };
        CoverageAnalyser sut = new(options, mockFileFinder.Object, mockParserFactory.Object, mockGitService.Object, Mock.Of<IDeltaCoverageService>(), Mock.Of<ICoverageRegressionService>());

        Coverage result = sut.AnalyseCoverage();

        Assert.That(result.Files, Has.Count.EqualTo(1));
    }

    [Test]
    public void AnalyseCoverageShouldExcludeFilesWhenExcludeIsUsed()
    {
        Mock<IFileFinder> mockFileFinder = new();
        Mock<IParserFactory> mockParserFactory = new();
        Mock<IGitService> mockGitService = new();
        Mock<ICoverageParser> mockParser = new();

        string currentDir = Environment.CurrentDirectory;
        string file1 = Path.Combine(currentDir, "src/File1.cs");
        string file2 = Path.Combine(currentDir, "tests/File2.cs");
        mockFileFinder.Setup(f => f.FindFiles(ValidDirectory)).Returns(["coverage.xml"]);

        mockParserFactory.Setup(f => f.CreateParser(It.IsAny<CoverageFormat>(), It.IsAny<Coverage>(), It.IsAny<Microsoft.Extensions.Logging.ILoggerFactory>()))
                         .Callback<CoverageFormat, Coverage, Microsoft.Extensions.Logging.ILoggerFactory>((_, c, _) =>
                         {
                             c.GetOrCreateFile(file1);
                             c.GetOrCreateFile(file2);
                         })
                         .Returns(mockParser.Object);

        // Filter: exclude tests/**
        CoverageAnalyserOptions options = CreateDefaultOptions() with { Exclude = ["tests/**"] };
        CoverageAnalyser sut = new(options, mockFileFinder.Object, mockParserFactory.Object, mockGitService.Object, Mock.Of<IDeltaCoverageService>(), Mock.Of<ICoverageRegressionService>());

        Coverage result = sut.AnalyseCoverage();

        Assert.That(result.Files, Has.Count.EqualTo(1));
        Assert.That(result.Files[0].Path, Is.EqualTo(file1));
    }

    [Test]
    public void FilterFilesShouldHandleEmptyIncludeAndExcludeLists()
    {
        Mock<IFileFinder> mockFileFinder = new();
        Mock<IParserFactory> mockParserFactory = new();
        Mock<IGitService> mockGitService = new();
        Mock<ICoverageParser> mockParser = new();

        string currentDir = Environment.CurrentDirectory;
        string file1 = Path.Combine(currentDir, "src/File1.cs");
        mockFileFinder.Setup(f => f.FindFiles(ValidDirectory)).Returns(["coverage.xml"]);

        mockParserFactory.Setup(f => f.CreateParser(It.IsAny<CoverageFormat>(), It.IsAny<Coverage>(), It.IsAny<Microsoft.Extensions.Logging.ILoggerFactory>()))
                         .Callback<CoverageFormat, Coverage, Microsoft.Extensions.Logging.ILoggerFactory>((_, c, _) =>
                         {
                             c.GetOrCreateFile(file1);
                         })
                         .Returns(mockParser.Object);

        // Filter with empty lists
        CoverageAnalyserOptions options = CreateDefaultOptions() with { Include = [], Exclude = [] };
        CoverageAnalyser sut = new(options, mockFileFinder.Object, mockParserFactory.Object, mockGitService.Object, Mock.Of<IDeltaCoverageService>(), Mock.Of<ICoverageRegressionService>());

        Coverage result = sut.AnalyseCoverage();

        Assert.That(result.Files, Has.Count.EqualTo(1));
    }
}
