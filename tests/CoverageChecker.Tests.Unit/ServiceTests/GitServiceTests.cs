using CoverageChecker.Services;
using CoverageChecker.Utils;
using Moq;

namespace CoverageChecker.Tests.Unit.ServiceTests;

public class GitServiceTests
{
    private class MockProcessExecutor : IProcessExecutor
    {
        public string RepoRoot { get; set; } = "/repo";
        public string DiffOutput { get; set; } = "";
        public int RepoRootExitCode { get; set; }
        public int DiffExitCode { get; set; }
        public string Stderr { get; set; } = "";

        public (int ExitCode, string StandardOutput, string StandardError) Execute(string fileName, IEnumerable<string> arguments, string? workingDirectory = null, TimeSpan? timeout = null)
        {
            List<string> argsList = arguments.ToList();

            if (argsList.Contains("rev-parse") && argsList.Contains("--show-toplevel"))
            {
                return (RepoRootExitCode, RepoRoot, Stderr);
            }

            // Updated check to match the robust command arguments
            return argsList.Contains("diff") && argsList.Contains("-U0") && argsList.Contains("--src-prefix=a/") && argsList.Contains("--dst-prefix=b/")
                ? (DiffExitCode, DiffOutput, Stderr)
                : (1, "", "Unknown arguments");
        }
    }

    private MockProcessExecutor _mockExecutor;
    private GitService _sut;

    [SetUp]
    public void Setup()
    {
        _mockExecutor = new MockProcessExecutor();
        _sut = new GitService(_mockExecutor);
    }

    [Test]
    public void GetChangedLines_ShouldHandleDeletedFiles_Correctly()
    {
        _mockExecutor.RepoRoot = TestContext.CurrentContext.TestDirectory;
        // Simulating:
        // 1. Modified file1.cs
        // 2. Deleted file2.cs (should be ignored)
        // 3. Modified file3.cs (should be captured correctly)
        _mockExecutor.DiffOutput = """
            diff --git a/file1.cs b/file1.cs
            index 111..222 100644
            --- a/file1.cs
            +++ b/file1.cs
            @@ -10,0 +11 @@
            +public void NewMethod()
            diff --git a/file2.cs b/file2.cs
            deleted file mode 100644
            index 333..000
            --- a/file2.cs
            +++ /dev/null
            @@ -1,5 +0,0 @@
            -content
            diff --git a/file3.cs b/file3.cs
            index 444..555 100644
            --- a/file3.cs
            +++ b/file3.cs
            @@ -5,0 +6 @@
            +added line
            """;

        IDictionary<string, HashSet<int>> result = _sut.GetChangedLines("main");

        string path1 = PathUtils.NormalizePath(Path.GetFullPath(Path.Combine(_mockExecutor.RepoRoot, "file1.cs")));
        string path2 = PathUtils.NormalizePath(Path.GetFullPath(Path.Combine(_mockExecutor.RepoRoot, "file2.cs")));
        string path3 = PathUtils.NormalizePath(Path.GetFullPath(Path.Combine(_mockExecutor.RepoRoot, "file3.cs")));

        Assert.Multiple(() =>
        {
            Assert.That(result, Contains.Key(path1), "file1 should be present");
            Assert.That(result[path1], Is.EquivalentTo((int[])[11]), "file1 changes");

            Assert.That(result, Does.Not.ContainKey(path2), "file2 (deleted) should not be present");

            Assert.That(result, Contains.Key(path3), "file3 should be present");
            Assert.That(result[path3], Is.EquivalentTo((int[])[6]), "file3 changes");
        });
    }

    [Test]
    public void GetChangedLines_ShouldParseSingleLineChange()
    {
        _mockExecutor.RepoRoot = TestContext.CurrentContext.TestDirectory;
        _mockExecutor.DiffOutput = """
            diff --git a/file1.cs b/file1.cs
            index ...
            --- a/file1.cs
            +++ b/file1.cs
            @@ -10,0 +11 @@
            +public void NewMethod()
            """;

        IDictionary<string, HashSet<int>> result = _sut.GetChangedLines("main");

        string expectedPath = PathUtils.NormalizePath(Path.GetFullPath(Path.Combine(_mockExecutor.RepoRoot, "file1.cs")));
        Assert.That(result, Contains.Key(expectedPath));
        Assert.That(result[expectedPath], Is.EquivalentTo((int[])[11]));
    }

    [Test]
    public void GetChangedLines_ShouldParseMultiLineChange()
    {
        _mockExecutor.RepoRoot = TestContext.CurrentContext.TestDirectory;
        _mockExecutor.DiffOutput = """
            +++ b/file1.cs
            @@ -20,3 +20,2 @@
            +line20
            +line21
            """;

        IDictionary<string, HashSet<int>> result = _sut.GetChangedLines("main");

        string expectedPath = PathUtils.NormalizePath(Path.GetFullPath(Path.Combine(_mockExecutor.RepoRoot, "file1.cs")));
        Assert.That(result[expectedPath], Is.EquivalentTo((int[])[20, 21]));
    }

    [Test]
    public void GetChangedLines_ShouldHandleFilesWithSpaces()
    {
        _mockExecutor.RepoRoot = TestContext.CurrentContext.TestDirectory;
        _mockExecutor.DiffOutput = """
            +++ "b/file name with spaces.cs"
            @@ -1,1 +1,1 @@
            +change1
            """;

        IDictionary<string, HashSet<int>> result = _sut.GetChangedLines("main");

        string expectedPath = PathUtils.NormalizePath(Path.GetFullPath(Path.Combine(_mockExecutor.RepoRoot, "file name with spaces.cs")));
        Assert.That(result, Contains.Key(expectedPath));
        Assert.That(result[expectedPath], Is.EquivalentTo((int[])[1]));
    }

    [Test]
    public void GetChangedLines_ShouldHandleEscapedFilenames()
    {
        _mockExecutor.RepoRoot = TestContext.CurrentContext.TestDirectory;
        // Git escapes quotes with backslashes when filenames are quoted
        _mockExecutor.DiffOutput = """
            +++ "b/file \"name\" with quotes.cs"
            @@ -1,1 +1,1 @@
            +change1
            """;

        IDictionary<string, HashSet<int>> result = _sut.GetChangedLines("main");

        string expectedPath = PathUtils.NormalizePath(Path.GetFullPath(Path.Combine(_mockExecutor.RepoRoot, "file \"name\" with quotes.cs")));
        Assert.That(result, Contains.Key(expectedPath));
        Assert.That(result[expectedPath], Is.EquivalentTo((int[])[1]));
    }

    [Test]
    public void GetChangedLines_ShouldHandleMultipleFiles()
    {
        _mockExecutor.RepoRoot = TestContext.CurrentContext.TestDirectory;
        _mockExecutor.DiffOutput = """
            +++ b/file1.cs
            @@ -1,1 +1,1 @@
            +change1
            +++ b/file2.cs
            @@ -5,1 +5,2 @@
            +change2
            +change3
            """;

        IDictionary<string, HashSet<int>> result = _sut.GetChangedLines("main");

        string path1 = PathUtils.NormalizePath(Path.GetFullPath(Path.Combine(_mockExecutor.RepoRoot, "file1.cs")));
        string path2 = PathUtils.NormalizePath(Path.GetFullPath(Path.Combine(_mockExecutor.RepoRoot, "file2.cs")));
        Assert.Multiple(() =>
        {
            Assert.That(result[path1], Is.EquivalentTo((int[])[1]));
            Assert.That(result[path2], Is.EquivalentTo((int[])[5, 6]));
        });
    }

    [Test]
    public void GetChangedLines_ShouldHandleEmptyDiff()
    {
        _mockExecutor.DiffOutput = "";

        IDictionary<string, HashSet<int>> result = _sut.GetChangedLines("main");

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetChangedLines_ShouldNotParseInvalidOctalSequences()
    {
        _mockExecutor.RepoRoot = TestContext.CurrentContext.TestDirectory;
        // \080 is not a valid octal sequence (8 is invalid).
        _mockExecutor.DiffOutput = """
            +++ "b/file\080name.cs"
            @@ -1,1 +1,1 @@
            +change1
            """;

        IDictionary<string, HashSet<int>> result = _sut.GetChangedLines("main");

        // It should be treated as literal \080 or at least not fail.
        // Based on the logic, it will hit the 'else' and append '0' then continue.
        string expectedPath = PathUtils.GetNormalizedFullPath(Path.Combine(_mockExecutor.RepoRoot, "file080name.cs"));
        Assert.That(result, Contains.Key(expectedPath));
    }

    [Test]
    public void GetChangedLines_ShouldThrowGitException_WhenGitNotFound()
    {
        Mock<IProcessExecutor> mockExecutor = new();
        mockExecutor.Setup(e => e.Execute(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>()))
                    .Throws(new System.ComponentModel.Win32Exception(2, "The system cannot find the file specified"));

        GitService sut = new(mockExecutor.Object);

        GitException? ex = Assert.Throws<GitException>(() => sut.GetChangedLines("main"));
        Assert.Multiple(() =>
        {
            Assert.That(ex.Message, Does.Contain("Failed to execute 'git'"));
            Assert.That(ex.InnerException, Is.TypeOf<System.ComponentModel.Win32Exception>());
        });
    }

    [Test]
    public void GetRepoRoot_ShouldThrowGitException_WhenGitNotFound()
    {
        Mock<IProcessExecutor> mockExecutor = new();
        mockExecutor.Setup(e => e.Execute(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>()))
                    .Throws(new System.ComponentModel.Win32Exception(2, "The system cannot find the file specified"));

        GitService sut = new(mockExecutor.Object);

        GitException? ex = Assert.Throws<GitException>(() => sut.GetRepoRoot());
        Assert.Multiple(() =>
        {
            Assert.That(ex.Message, Does.Contain("Failed to execute 'git'"));
            Assert.That(ex.InnerException, Is.TypeOf<System.ComponentModel.Win32Exception>());
        });
    }

    [Test]
    public void DefaultConstructor_ShouldNotThrow()
    {
        Assert.DoesNotThrow(() => _ = new GitService());
    }

    [Test]
    public void GetChangedLines_ShouldThrowOnGitDiffError()
    {
        _mockExecutor.DiffExitCode = 1;
        _mockExecutor.Stderr = "fatal: some git error";

        GitException? ex = Assert.Throws<GitException>(() => _sut.GetChangedLines("main"));
        Assert.That(ex.Message, Does.Contain("Git diff failed with exit code 1"));
    }

    [Test]
    public void GetChangedLines_ShouldThrowOnGetRepoRootError()
    {
        _mockExecutor.RepoRootExitCode = 1;
        _mockExecutor.Stderr = "fatal: not a git repository";

        GitException? ex = Assert.Throws<GitException>(() => _sut.GetChangedLines("main"));
        Assert.That(ex.Message, Does.Contain("Failed to get git repo root"));
    }

    [Test]
    public void GetChangedLines_ShouldThrow_WhenBaseStartsWithDash()
    {
        Assert.Throws<ArgumentException>(() => _sut.GetChangedLines("-malicious-flag"));
    }

    [Test]
    public void GetChangedLines_ShouldThrow_WhenHeadStartsWithDash()
    {
        Assert.Throws<ArgumentException>(() => _sut.GetChangedLines("main", "-malicious-flag"));
    }

    [Test]
    public void GetChangedLines_ShouldHandleMultiByteOctalEscapes()
    {
        _mockExecutor.RepoRoot = TestContext.CurrentContext.TestDirectory;
        // snowman is \342\230\203 in octal (UTF-8)
        _mockExecutor.DiffOutput = """
            +++ "b/file\342\230\203.cs"
            @@ -1,1 +1,1 @@
            +change1
            """;

        IDictionary<string, HashSet<int>> result = _sut.GetChangedLines("main");

        string expectedPath = PathUtils.GetNormalizedFullPath(Path.Combine(_mockExecutor.RepoRoot, "file☃.cs"));
        Assert.That(result, Contains.Key(expectedPath));
    }

    [Test]
    public void GetChangedLines_ShouldHandleTabsAndTimestampsInHeader()
    {
        _mockExecutor.RepoRoot = TestContext.CurrentContext.TestDirectory;
        // Some git setups output: +++ b/file.cs       2023-01-01 12:00:00.000000000 +0100
        _mockExecutor.DiffOutput = """
            +++ b/file.cs      2023-01-01 12:00:00.000000000 +0100
            @@ -1,1 +1,1 @@
            +change1
            """;

        IDictionary<string, HashSet<int>> result = _sut.GetChangedLines("main");

        string expectedPath = PathUtils.GetNormalizedFullPath(Path.Combine(_mockExecutor.RepoRoot, "file.cs"));
        Assert.That(result, Contains.Key(expectedPath));
    }

    [Test]
    public void GetChangedLines_ShouldHandleComplexQuotes()
    {
        _mockExecutor.RepoRoot = TestContext.CurrentContext.TestDirectory;
        // Git escapes quotes with backslashes.
        // Original: file "name" mid.cs
        // Escaped: "file \"name\" mid.cs"
        _mockExecutor.DiffOutput = """
            +++ "b/file \"name\" mid.cs"
            @@ -1,1 +1,1 @@
            +change1
            """;

        IDictionary<string, HashSet<int>> result = _sut.GetChangedLines("main");

        string expectedPath = PathUtils.NormalizePath(Path.GetFullPath(Path.Combine(_mockExecutor.RepoRoot, "file \"name\" mid.cs")));
        Assert.That(result, Contains.Key(expectedPath));
        Assert.That(result[expectedPath], Is.EquivalentTo((int[])[1]));
    }

    [Test]
    public void GetChangedLines_ShouldHandleUnicodeFileNames()
    {
        _mockExecutor.RepoRoot = TestContext.CurrentContext.TestDirectory;
        // unicode snow man: ☃
        string filename = "file☃.cs";
        _mockExecutor.DiffOutput = $"""
            +++ b/{filename}
            @@ -1,1 +1,1 @@
            +change1
            """;

        IDictionary<string, HashSet<int>> result = _sut.GetChangedLines("main");

        string expectedPath = PathUtils.NormalizePath(Path.GetFullPath(Path.Combine(_mockExecutor.RepoRoot, filename)));
        Assert.That(result, Contains.Key(expectedPath));
        Assert.That(result[expectedPath], Is.EquivalentTo((int[])[1]));
    }

    [Test]
    public void GetChangedLines_ShouldHandleFileNamesWithGitPrefixPath()
    {
        _mockExecutor.RepoRoot = TestContext.CurrentContext.TestDirectory;
        // File named "b/file.cs" inside the repo
        string filename = "b/file.cs";
        _mockExecutor.DiffOutput = """
            +++ b/b/file.cs
            @@ -1,1 +1,1 @@
            +change1
            """;

        IDictionary<string, HashSet<int>> result = _sut.GetChangedLines("main");

        string expectedPath = PathUtils.NormalizePath(Path.GetFullPath(Path.Combine(_mockExecutor.RepoRoot, filename)));
        Assert.That(result, Contains.Key(expectedPath));
    }

    [Test]
    public void GetChangedLines_ShouldHandleFileNamesStartingWithPlus()
    {
        _mockExecutor.RepoRoot = TestContext.CurrentContext.TestDirectory;
        string filename = "+file.cs";
        _mockExecutor.DiffOutput = """
            +++ b/+file.cs
            @@ -1,1 +1,1 @@
            +change1
            """;

        IDictionary<string, HashSet<int>> result = _sut.GetChangedLines("main");

        string expectedPath = PathUtils.NormalizePath(Path.GetFullPath(Path.Combine(_mockExecutor.RepoRoot, filename)));
        Assert.That(result, Contains.Key(expectedPath));
    }

    [Test]
    public void GetChangedLines_ShouldAllowValidGitReferences()
    {
        Assert.DoesNotThrow(() => _sut.GetChangedLines("HEAD^"));
        Assert.DoesNotThrow(() => _sut.GetChangedLines("feature/branch-name"));
        Assert.DoesNotThrow(() => _sut.GetChangedLines("a1b2c3d"));
    }

    [Test]
    public void GetChangedLines_ShouldHandleTrailingBackslashInFilename()
    {
        _mockExecutor.RepoRoot = TestContext.CurrentContext.TestDirectory;
        // Filename ending with a backslash
        _mockExecutor.DiffOutput = """
            +++ "b/file\\"
            @@ -1,1 +1,1 @@
            +change1
            """;

        IDictionary<string, HashSet<int>> result = _sut.GetChangedLines("main");

        string expectedPath = PathUtils.GetNormalizedFullPath(Path.Combine(_mockExecutor.RepoRoot, "file\\"));
        Assert.That(result, Contains.Key(expectedPath));
    }

    [Test]
    public void GetChangedLines_ShouldHandleBinaryFiles()
    {
        _mockExecutor.RepoRoot = TestContext.CurrentContext.TestDirectory;
        _mockExecutor.DiffOutput = """
            diff --git a/image.png b/image.png
            new file mode 100644
            index 0000000..1234567
            Binary files /dev/null and b/image.png differ
            """;

        IDictionary<string, HashSet<int>> result = _sut.GetChangedLines("main");

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetChangedLines_ShouldThrow_WhenBaseIsJustDash()
    {
        Assert.Throws<ArgumentException>(() => _sut.GetChangedLines("-"));
    }

    [Test]
    public void GetChangedLines_ShouldHandleTightSpacingInHunkHeader()
    {
        _mockExecutor.RepoRoot = TestContext.CurrentContext.TestDirectory;
        _mockExecutor.DiffOutput = """
            +++ b/file.cs
            @@-1,1 +1,1@@
            +change1
            """;

        IDictionary<string, HashSet<int>> result = _sut.GetChangedLines("main");

        string expectedPath = PathUtils.GetNormalizedFullPath(Path.Combine(_mockExecutor.RepoRoot, "file.cs"));
        Assert.That(result, Contains.Key(expectedPath));
        Assert.That(result[expectedPath], Is.EquivalentTo((int[])[1]));
    }

    [Test]
    public void GetChangedLines_ShouldHandleVariousEscapeSequences()
    {
        _mockExecutor.RepoRoot = TestContext.CurrentContext.TestDirectory;
        // Git escapes: \t, \n, \b, \f, \r, \v, \a, \e (escape), \\, invalid \z, non-octal \8, partial octal \3 (end), partial octal \34z
        _mockExecutor.DiffOutput = """
            +++ "b/tab\tnewline\nbackspace\bformfeed\freturn\rvertical\vbell\aescape\eunknown\znum\8end\3mid\34z"
            @@ -1 +1 @@
            +change
            """;

        IDictionary<string, HashSet<int>> result = _sut.GetChangedLines("main");

        Assert.That(result, Is.Not.Empty);
        string path = result.Keys.First();
        
        // Verify path contains decoded characters
        Assert.That(path, Does.Contain("\t"), "Tab");
        Assert.That(path, Does.Contain("\n"), "Newline");
        Assert.That(path, Does.Contain("\b"), "Backspace");
        Assert.That(path, Does.Contain("\f"), "Formfeed");
        Assert.That(path, Does.Contain("\r"), "CarriageReturn");
        Assert.That(path, Does.Contain("\v"), "VerticalTab");
        Assert.That(path, Does.Contain("\a"), "Bell");
        Assert.That(path, Does.Contain("\u001b"), "Escape");
        Assert.That(path, Does.Contain("unknownz"), "Unknown escape should strip backslash");
        Assert.That(path, Does.Contain("num8"), "Non-octal digit should be literal");
        Assert.That(path, Does.Contain("end3"), "Partial octal at end should be literal");
        Assert.That(path, Does.Contain("mid34z"), "Partial octal mid-string should be literal");
    }

    [Test]
    public void GetChangedLines_ShouldHandleCompactHunkHeader()
    {
        _mockExecutor.RepoRoot = TestContext.CurrentContext.TestDirectory;
        // Compact header @@ -1 +1 @@ implies 1 line context/change
        _mockExecutor.DiffOutput = """
            +++ b/file.cs
            @@ -1 +1 @@
            +change
            """;

        IDictionary<string, HashSet<int>> result = _sut.GetChangedLines("main");

        string expectedPath = PathUtils.GetNormalizedFullPath(Path.Combine(_mockExecutor.RepoRoot, "file.cs"));
        Assert.That(result, Contains.Key(expectedPath));
        Assert.That(result[expectedPath], Is.EquivalentTo((int[])[1]));
    }

    [Test]
    public void DefaultConstructor_ShouldInitializeCorrectly()
    {
        GitService sut = new();
        Assert.That(sut, Is.Not.Null);
    }

    [Test]
    public void GetChangedLines_ShouldHandleSameFileAppearingTwice()
    {
        _mockExecutor.RepoRoot = TestContext.CurrentContext.TestDirectory;
        // Same file appearing twice
        _mockExecutor.DiffOutput = """
            +++ b/file.cs
            @@ -1 +1 @@
            +change1
            +++ b/file.cs
            @@ -5 +5 @@
            +change2
            """;

        IDictionary<string, HashSet<int>> result = _sut.GetChangedLines("main");

        string expectedPath = PathUtils.GetNormalizedFullPath(Path.Combine(_mockExecutor.RepoRoot, "file.cs"));
        Assert.That(result, Contains.Key(expectedPath));
        Assert.That(result[expectedPath], Is.EquivalentTo((int[])[1, 5]));
    }

    [Test]
    public void GetChangedLines_ShouldThrowGitException_WhenGitDiffFailsWithWin32Exception()
    {
        Mock<IProcessExecutor> mockExecutor = new();
        // Setup GetRepoRoot to succeed
        mockExecutor.Setup(e => e.Execute("git", It.Is<IEnumerable<string>>(a => a.Contains("rev-parse")), It.IsAny<string?>(), It.IsAny<TimeSpan?>()))
                    .Returns((0, "/repo", ""));
        
        // Setup diff to throw Win32Exception
        mockExecutor.Setup(e => e.Execute("git", It.Is<IEnumerable<string>>(a => a.Contains("diff")), It.IsAny<string?>(), It.IsAny<TimeSpan?>()))
                    .Throws(new System.ComponentModel.Win32Exception(2, "The system cannot find the file specified"));

        GitService sut = new(mockExecutor.Object);

        GitException? ex = Assert.Throws<GitException>(() => sut.GetChangedLines("main"));
        Assert.Multiple(() =>
        {
            Assert.That(ex.Message, Does.Contain("Failed to execute 'git'"));
            Assert.That(ex.InnerException, Is.TypeOf<System.ComponentModel.Win32Exception>());
        });
    }
}