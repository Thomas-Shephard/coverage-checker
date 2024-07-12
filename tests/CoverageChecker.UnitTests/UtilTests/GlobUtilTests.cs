using CoverageChecker.Utils;
using Microsoft.Extensions.FileSystemGlobbing;
using Moq;

namespace CoverageChecker.UnitTests.UtilTests;

public class GlobUtilTests {
    [TestCase]
    [TestCase("*")]
    [TestCase("**/*.cs")]
    [TestCase("!**/obj/**", "!**/obj/**", "**/*.cs", "!**/bin/**")]
    [TestCase("**/*.cs", "!**/obj/**", "!**/bin/**")]
    [TestCase("!**/obj/**", "**/*.cs", "**/*.csproj", "!**/bin/**", "!**/obj/**")]
    public void GlobUtils_AddFromGlobPatterns_AddsPatterns(params string[] globPatterns) {
        Mock<Matcher> matcher = new();
        matcher.Object.AddFromGlobPatterns(globPatterns);

        IEnumerable<(string globPattern, int occurrences)> globPatternOccurrences = globPatterns.GroupBy(g => g)
                                                                                                .Select(g => (g.Key, g.Count()));

        foreach ((string globPattern, int occurrences) in globPatternOccurrences) {
            if (globPattern.StartsWith('!')) {
                string expectedGlobalPattern = globPattern[1..];
                matcher.Verify(m => m.AddExclude(expectedGlobalPattern), Times.Exactly(occurrences));
            } else {
                matcher.Verify(m => m.AddInclude(globPattern), Times.Exactly(occurrences));
            }
        }

        matcher.VerifyNoOtherCalls();
    }

    [Test]
    public void GlobUtils_AddFromGlobPatterns_EmptyPatterns_DoesNotAddPatterns() {
        Mock<Matcher> matcher = new();
        matcher.Object.AddFromGlobPatterns([]);

        matcher.Verify(m => m.AddInclude(It.IsAny<string>()), Times.Never);
        matcher.Verify(m => m.AddExclude(It.IsAny<string>()), Times.Never);
    }
}