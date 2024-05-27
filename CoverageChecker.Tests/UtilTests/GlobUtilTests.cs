using CoverageChecker.Utils;
using Microsoft.Extensions.FileSystemGlobbing;
using Moq;

namespace CoverageChecker.Tests.UtilTests;

public class GlobUtilTests {
    [TestCase("**/*.cs")]
    [TestCase("!**/obj/**")]
    [TestCase("**/*.cs", "!**/obj/**", "!**/bin/**")]
    [TestCase("!**/obj/**", "**/*.cs", "**/*.csproj")]
    public void GlobUtils_CreateFromGlobPatterns_CreatesMatcher(params string[] globPatterns) {
        // Mock the Matcher class to verify that the correct AddInclude and AddExclude calls are made
        Mock<Matcher> matcher = new();

        GlobUtils.CreateFromGlobPatterns(globPatterns, matcher.Object);

        // Separate the include and exclude patterns into separate arrays
        string[] expectedIncludePatterns = globPatterns
                                           .Where(p => !p.StartsWith('!'))
                                           .ToArray();

        string[] expectedExcludePatterns = globPatterns
                                           .Where(p => p.StartsWith('!'))
                                           .Select(p => p[1..])
                                           .ToArray();

        // Verify that the correct number of AddInclude and AddExclude calls are made with the correct arguments
        matcher.Verify(m => m.AddInclude(It.IsIn(expectedIncludePatterns)), Times.Exactly(expectedIncludePatterns.Length));
        matcher.Verify(m => m.AddExclude(It.IsIn(expectedExcludePatterns)), Times.Exactly(expectedExcludePatterns.Length));
    }

    [Test]
    public void GlobUtils_CreateFromGlobPatterns_EmptyPatterns_CreatesMatcher() {
        Matcher matcher = GlobUtils.CreateFromGlobPatterns([]);

        // Ensure that the matcher is not null when no patterns are provided
        Assert.That(matcher, Is.Not.Null);
    }

    [Test]
    public void GlobUtils_CreateFromGlobPatterns_AllPatterns_CreatesMatcher() {
        Matcher matcher = GlobUtils.CreateFromGlobPatterns(["*"]);

        string[] actual = matcher.GetResultsInFullPath(Environment.CurrentDirectory).ToArray();
        string[] expected = Directory.GetFiles(Environment.CurrentDirectory);

        // Ensure that the matcher found at least one file
        Assert.That(actual, Is.Not.Empty);
        // Ensure that the matcher found all files in the current directory
        Assert.That(actual, Is.EquivalentTo(expected));
    }
}