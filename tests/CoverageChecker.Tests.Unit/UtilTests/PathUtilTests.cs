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
}