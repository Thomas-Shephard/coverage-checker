using CoverageChecker.Services;
using CoverageChecker.Utils;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoverageChecker.Tests.Unit.ServiceTests;

public class FileFinderTests
{
    private string _tempDirectory;

    [SetUp]
    public void SetUp()
    {
        _tempDirectory = PathUtils.NormalizePath(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        Directory.CreateDirectory(_tempDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Test]
    public void ConstructorWithLoggerDoesNotThrow()
    {
        Assert.DoesNotThrow(() => _ = new FileFinder(["**/*.xml"], NullLogger<FileFinder>.Instance));
    }

    [Test]
    public void ConstructorWithMatcherAndLoggerDoesNotThrow()
    {
        Matcher matcher = new();
        matcher.AddInclude("**/*.xml");
        Assert.DoesNotThrow(() => _ = new FileFinder(matcher, NullLogger<FileFinder>.Instance));
    }

    [Test]
    public void ConstructorEmptyGlobPatternsThrowsArgumentException()
    {
        Exception e = Assert.Throws<ArgumentException>(() => _ = new FileFinder([]));
        Assert.That(e.Message, Does.StartWith("At least one glob pattern must be provided"));
    }

    [Test]
    public void FindFilesWithGlobPatternsReturnsMatchingFiles()
    {
        CreateFile("file1.xml");
        CreateFile("sub/file2.xml");
        CreateFile("image.png");

        FileFinder fileFinder = new(["**/*.xml"]);

        string[] files = fileFinder.FindFiles(_tempDirectory).ToArray();

        Assert.That(files, Has.Length.EqualTo(2));
        Assert.That(files, Contains.Item(PathUtils.NormalizePath(Path.Combine(_tempDirectory, "file1.xml"))));
        Assert.That(files, Contains.Item(PathUtils.NormalizePath(Path.Combine(_tempDirectory, "sub", "file2.xml"))));
    }

    [Test]
    public void FindFilesWithMatcherReturnsMatchingFiles()
    {
        CreateFile("file1.xml");
        CreateFile("sub/file2.xml");
        CreateFile("image.png");

        Matcher matcher = new();
        matcher.AddInclude("**/*.xml");
        FileFinder fileFinder = new(matcher);

        string[] files = fileFinder.FindFiles(_tempDirectory).ToArray();

        Assert.That(files, Has.Length.EqualTo(2));
        Assert.That(files, Contains.Item(PathUtils.NormalizePath(Path.Combine(_tempDirectory, "file1.xml"))));
        Assert.That(files, Contains.Item(PathUtils.NormalizePath(Path.Combine(_tempDirectory, "sub", "file2.xml"))));
    }

    [Test]
    public void FindFilesWithExclusionPatternReturnsMatchingFiles()
    {
        CreateFile("file1.xml");
        CreateFile("sub/file2.xml");
        CreateFile("obj/file3.xml");

        FileFinder fileFinder = new(["**/*.xml", "!obj/**"]);

        string[] files = fileFinder.FindFiles(_tempDirectory).ToArray();

        Assert.That(files, Has.Length.EqualTo(2));
        Assert.That(files, Contains.Item(PathUtils.NormalizePath(Path.Combine(_tempDirectory, "file1.xml"))));
        Assert.That(files, Contains.Item(PathUtils.NormalizePath(Path.Combine(_tempDirectory, "sub", "file2.xml"))));
        Assert.That(files, Does.Not.Contain(PathUtils.NormalizePath(Path.Combine(_tempDirectory, "obj", "file3.xml"))));
    }

    [Test]
    public void FindFilesNoMatchingFilesReturnsEmpty()
    {
        CreateFile("image.png");

        FileFinder fileFinder = new(["**/*.xml"]);

        IEnumerable<string> files = fileFinder.FindFiles(_tempDirectory);

        Assert.That(files, Is.Empty);
    }

    private void CreateFile(string relativePath)
    {
        string fullPath = Path.Combine(_tempDirectory, relativePath);
        string? directory = Path.GetDirectoryName(fullPath);
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(fullPath, string.Empty);
    }
}
