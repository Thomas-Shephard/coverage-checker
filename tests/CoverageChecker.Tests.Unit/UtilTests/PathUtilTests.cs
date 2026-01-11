using CoverageChecker.Utils;

namespace CoverageChecker.Tests.Unit.UtilTests;

public class PathUtilTests
{
    [TestCase("path/to/file", "path/to/file")]
    [TestCase(@"path\to\file", "path/to/file")]
    [TestCase(@"path\to/file", "path/to/file")]
    [TestCase("path/to/file/", "path/to/file")]
    [TestCase(@"path\to\file\", "path/to/file")]
    [TestCase("/", "/")]
    [TestCase("C:", "C:/")]
    [TestCase(@"C:\", "C:/")]
    [TestCase(@"\\server\share\", "//server/share")]
    [TestCase("", "")]
    public void NormalizePath_NormalizesSeparatorsAndRemovesTrailingSlashes(string input, string expected)
    {
        string result = PathUtils.NormalizePath(input);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void NormalizePath_ThrowsIfNull()
    {
        // ReSharper disable once NullableWarningSuppressionIsUsed
        Assert.That(() => PathUtils.NormalizePath(null!), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void GetNormalizedFullPath_ReturnsNormalizedPath()
    {
        string path = "test/path";
        string result = PathUtils.GetNormalizedFullPath(path);

        Assert.That(result, Is.EqualTo(PathUtils.NormalizePath(Path.GetFullPath(path))));
    }

    [Test]
    public void GetNormalizedFullPath_ResolvesRelativePathAgainstBase()
    {
        string basePath = @"C:\base";
        string relativePath = "subdir/file.cs";
        string result = PathUtils.GetNormalizedFullPath(relativePath, basePath);

        Assert.That(result, Is.EqualTo("C:/base/subdir/file.cs"));
    }

    [Test]
    public void GetNormalizedFullPath_ThrowsArgumentException_WhenPathIsInvalid()
    {
        string invalidPath = "invalid\0path";
        
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => PathUtils.GetNormalizedFullPath(invalidPath));
        Assert.That(ex.Message, Does.Contain("Invalid path"));
    }
}