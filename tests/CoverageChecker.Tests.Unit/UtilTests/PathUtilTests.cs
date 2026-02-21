using CoverageChecker.Utils;

namespace CoverageChecker.Tests.Unit.UtilTests;

internal sealed class PathUtilTests
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
    public void NormalizePathNormalizesSeparatorsAndRemovesTrailingSlashes(string input, string expected)
    {
        string result = PathUtils.NormalizePath(input);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void NormalizePathThrowsIfNull()
    {
        // ReSharper disable once NullableWarningSuppressionIsUsed
        Assert.That(() => PathUtils.NormalizePath(null!), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void GetNormalizedFullPathReturnsNormalizedPath()
    {
        string path = "test/path";
        string result = PathUtils.GetNormalizedFullPath(path);

        Assert.That(result, Is.EqualTo(PathUtils.NormalizePath(Path.GetFullPath(path))));
    }

    [TestCase(PathStyle.Windows, @"C:\base", "subdir/file.cs", "C:/base/subdir/file.cs")]
    [TestCase(PathStyle.Unix, "/base", "subdir/file.cs", "/base/subdir/file.cs")]
    [TestCase(PathStyle.Windows, @"\\server\share", "subdir/file.cs", @"//server/share/subdir/file.cs")]
    public void GetNormalizedFullPathResolvesRelativePathAgainstBase(PathStyle style, string basePath, string relativePath, string expected)
    {
        using (PathUtils.OverrideStyle(style))
        {
            string result = PathUtils.GetNormalizedFullPath(relativePath, basePath);
            Assert.That(result, Is.EqualTo(expected));
        }
    }

    [Test]
    public void GetNormalizedFullPathThrowsArgumentExceptionWhenPathIsInvalid()
    {
        string invalidPath = "invalid\0path";

        string result = PathUtils.GetNormalizedFullPath(invalidPath);
        Assert.That(result, Is.EqualTo("invalid\0path"));
    }

    [TestCase("", "")]
    [TestCase(null, null)]
    public void MakeRelativeIfInsideReturnsOriginalWhenPathIsNullOrEmpty(string? input, string? expected)
    {
        // ReSharper disable once NullableWarningSuppressionIsUsed
        Assert.That(PathUtils.MakeRelativeIfInside("dir", input!), Is.EqualTo(expected));
    }

    [TestCase("rel/path", "rel/path")]
    public void MakeRelativeIfInsideReturnsOriginalWhenNotRooted(string input, string expected)
    {
        Assert.That(PathUtils.MakeRelativeIfInside("dir", input), Is.EqualTo(expected));
    }

    [TestCase(PathStyle.Windows, @"C:\dir1", @"C:\dir2\file.txt", @"C:/dir2/file.txt")]
    [TestCase(PathStyle.Unix, "/dir1", "/dir2/file.txt", "/dir2/file.txt")]
    public void MakeRelativeIfInsideReturnsOriginalWhenPathIsNotInsideDirectory(PathStyle style, string dir, string path, string expected)
    {
        using (PathUtils.OverrideStyle(style))
        {
            string result = PathUtils.MakeRelativeIfInside(dir, path);
            Assert.That(result, Is.EqualTo(expected));
        }
    }

    [TestCase(PathStyle.Windows, @"C:\dir1", @"C:\dir1\subdir\file.txt", "subdir/file.txt")]
    [TestCase(PathStyle.Unix, "/dir1", "/dir1/subdir/file.txt", "subdir/file.txt")]
    [TestCase(PathStyle.Unix, "/dir1", "/dir1", ".")]
    public void MakeRelativeIfInsideReturnsRelativePathWhenInside(PathStyle style, string dir, string path, string expected)
    {
        using (PathUtils.OverrideStyle(style))
        {
            string result = PathUtils.MakeRelativeIfInside(dir, path);
            Assert.That(result, Is.EqualTo(expected));
        }
    }

    [TestCase(PathStyle.Windows, @"C:\dir", @"C:\dir")]
    [TestCase(PathStyle.Unix, "/dir", "/dir")]
    public void IsSubPathOfReturnsTrueForSamePath(PathStyle style, string dir, string other)
    {
        using (PathUtils.OverrideStyle(style))
        {
            Assert.That(PathUtils.IsSubPathOf(dir, other), Is.True);
        }
    }

    [TestCase(PathStyle.Windows, @"C:\dir", @"C:\dir\child")]
    [TestCase(PathStyle.Unix, "/dir", "/dir/child")]
    public void IsSubPathOfReturnsTrueForChild(PathStyle style, string dir, string child)
    {
        using (PathUtils.OverrideStyle(style))
        {
            Assert.That(PathUtils.IsSubPathOf(dir, child), Is.True);
        }
    }

    [TestCase(PathStyle.Windows, @"C:\dir", @"C:\dir_suffix")]
    [TestCase(PathStyle.Unix, "/dir", "/dir_suffix")]
    public void IsSubPathOfReturnsFalseForSuffixPath(PathStyle style, string dir, string suffix)
    {
        using (PathUtils.OverrideStyle(style))
        {
            Assert.That(PathUtils.IsSubPathOf(dir, suffix), Is.False);
        }
    }

    [TestCase(PathStyle.Windows, @"C:\dir1", @"C:\dir2")]
    [TestCase(PathStyle.Unix, "/dir1", "/dir2")]
    public void IsSubPathOfReturnsFalseForDifferentPaths(PathStyle style, string dir1, string dir2)
    {
        using (PathUtils.OverrideStyle(style))
        {
            Assert.That(PathUtils.IsSubPathOf(dir1, dir2), Is.False);
        }
    }

    [TestCase(PathStyle.Windows, @"C:\dir", @"c:\DIR\CHILD", true)]
    [TestCase(PathStyle.Unix, "/dir", "/DIR/CHILD", false)]
    public void IsSubPathOfHandlesCaseSensitivityBasedOnPathStyle(PathStyle style, string dir, string child, bool expected)
    {
        using (PathUtils.OverrideStyle(style))
        {
            Assert.That(PathUtils.IsSubPathOf(dir, child), Is.EqualTo(expected));
        }
    }

    [TestCase(null, false)]
    [TestCase("", false)]
    [TestCase("/", true)]
    [TestCase(@"\", true)]
    [TestCase(@"\\server\share", true)]
    [TestCase("//server/share", true)]
    [TestCase("C:", true)]
    [TestCase("C:/", true)]
    [TestCase("A", false)]
    [TestCase("AB", false)]
    [TestCase("1:", false)]
    [TestCase("rel/path", false)]
    [TestCase("C", false)]
    public void IsPathRootedChecksVariousPathStyles(string? path, bool expected)
    {
        Assert.That(PathUtils.IsPathRooted(path), Is.EqualTo(expected));
    }

    [Test]
    public void PathComparerReturnsAppropriateComparerForCurrentOs()
    {
        bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
        StringComparer expected = isWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        Assert.That(PathUtils.PathComparer, Is.EqualTo(expected));
    }

    [Test]
    public void IsSubPathOfWorksWithoutStyleOverride()
    {
        string dir = Path.GetTempPath();
        string child = Path.Combine(dir, "child");

        Assert.That(PathUtils.IsSubPathOf(dir, child), Is.True);
    }

    [Test]
    public void CurrentStyleDefaultsToUnixOnNonWindows()
    {
        using (PathUtils.OverrideOs(false))
        {
            Assert.That(PathUtils.PathComparer, Is.EqualTo(StringComparer.Ordinal));
        }
    }

    [Test]
    public void GetNormalizedFullPathUsesSimpleNormalizationOnMismatchedOsStyle()
    {
        using (PathUtils.OverrideOs(true))
        using (PathUtils.OverrideStyle(PathStyle.Unix))
        {
            string path = "/unix/path/file.txt";
            string result = PathUtils.GetNormalizedFullPath(path);

            Assert.That(result, Is.EqualTo("/unix/path/file.txt"));
        }
    }

    [Test]
    public void GetNormalizedFullPathUsesSimpleNormalizationOnMismatchedOsStyleWindowsOnUnix()
    {
        using (PathUtils.OverrideOs(false))
        using (PathUtils.OverrideStyle(PathStyle.Windows))
        {
            string path = @"C:\windows\path\file.txt";
            string result = PathUtils.GetNormalizedFullPath(path);

            Assert.That(result, Is.EqualTo("C:/windows/path/file.txt"));
        }
    }

    [Test]
    public void GetNormalizedFullPathCallsGetFullPathOnUnixWhenOsIsUnix()
    {
        using (PathUtils.OverrideOs(false))
        using (PathUtils.OverrideStyle(PathStyle.Unix))
        {
            string path = "relative/path";
            string result = PathUtils.GetNormalizedFullPath(path);

            Assert.That(result, Is.EqualTo(PathUtils.NormalizePath(Path.GetFullPath(path))));
        }
    }

    [Test]
    public void GetNormalizedFullPathHandlesCurrentOsStyleExplicitly()
    {
        using (PathUtils.OverrideOs(true))
        using (PathUtils.OverrideStyle(PathStyle.Windows))
        {
            string path = "relative/path";
            string result = PathUtils.GetNormalizedFullPath(path);

            Assert.That(result, Is.EqualTo(PathUtils.NormalizePath(Path.GetFullPath(path))));
        }
    }
}