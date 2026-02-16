using CoverageChecker.Utils;
using Microsoft.Extensions.FileSystemGlobbing;
using Moq;

namespace CoverageChecker.Tests.Unit.UtilTests;

public class GlobUtilTests
{
    [TestCase("*")]
    [TestCase("**/*.cs")]
    [TestCase("!**/obj/**", "!**/obj/**", "**/*.cs", "!**/bin/**")]
    [TestCase("**/*.cs", "!**/obj/**", "!**/bin/**")]
    [TestCase("!**/obj/**", "**/*.cs", "**/*.csproj", "!**/bin/**", "!**/obj/**")]
    public void GlobUtilsAddGlobPatternsAddsPatterns(params string[] globPatterns)
    {
        Mock<Matcher> matcher = new();
        matcher.Object.AddGlobPatterns(globPatterns);

        IEnumerable<(string globPattern, int occurrences)> globPatternOccurrences = globPatterns.GroupBy(g => g)
                                                                                                .Select(g => (g.Key, g.Count()));

        foreach ((string globPattern, int occurrences) in globPatternOccurrences)
        {
            if (globPattern.StartsWith('!'))
            {
                string expectedGlobalPattern = globPattern[1..];
                matcher.Verify(m => m.AddExclude(expectedGlobalPattern), Times.Exactly(occurrences));
            }
            else
            {
                matcher.Verify(m => m.AddInclude(globPattern), Times.Exactly(occurrences));
            }
        }

        matcher.VerifyNoOtherCalls();
    }

    [Test]
    public void GlobUtilsAddGlobPatternsAddsPatternsList()
    {
        List<string> globPatterns = ["**/*.cs", "!**/obj/**"];
        Mock<Matcher> matcher = new();
        matcher.Object.AddGlobPatterns(globPatterns);

        matcher.Verify(m => m.AddInclude("**/*.cs"), Times.Once);
        matcher.Verify(m => m.AddExclude("**/obj/**"), Times.Once);

        matcher.VerifyNoOtherCalls();
    }

    [Test]
    public void GlobUtilsAddGlobPatternsEmptyPatternsThrowsException()
    {
        Matcher matcher = new();
        Exception e = Assert.Throws<ArgumentException>(() => matcher.AddGlobPatterns([]));
        Assert.That(e.Message, Does.StartWith("At least one glob pattern must be provided"));
    }
}