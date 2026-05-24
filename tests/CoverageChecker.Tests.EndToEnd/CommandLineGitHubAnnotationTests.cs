namespace CoverageChecker.Tests.EndToEnd;

internal sealed class CommandLineGitHubAnnotationTests : CommandLineTestBase
{
    [Test]
    public async Task CheckCommandEmitsGitHubAnnotationForUncoveredLineWhenThresholdFails()
    {
        using TestDirectory testDirectory = new();
        File.WriteAllText(Path.Combine(testDirectory.Path, "coverage.xml"), CreateCoverageXml(testDirectory.Path, "src/Foo.cs", (10, 1), (11, 0), (12, 1)));
        string summaryPath = Path.Combine(testDirectory.Path, "summary.md");

        (int exitCode, string stdout) = await RunCliWithGitHubEnvironment(
            testDirectory.Path,
            summaryPath,
            "check",
            "--directory", testDirectory.Path,
            "--glob-patterns", "coverage.xml",
            "--format", "Cobertura",
            "--line-threshold", "100");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("::warning"));
            Assert.That(stdout, Does.Contain("file=src/Foo.cs"));
            Assert.That(stdout, Does.Contain("line=11"));
            Assert.That(stdout, Does.Contain("title=Missing Line Coverage"));
        }
    }

    [Test]
    public async Task CheckCommandEmitsGitHubAnnotationForUncoveredLineRangeWhenThresholdFails()
    {
        using TestDirectory testDirectory = new();
        File.WriteAllText(Path.Combine(testDirectory.Path, "coverage.xml"), CreateCoverageXml(testDirectory.Path, "src/Foo.cs", (10, 1), (11, 0), (12, 0), (13, 1)));
        string summaryPath = Path.Combine(testDirectory.Path, "summary.md");

        (int exitCode, string stdout) = await RunCliWithGitHubEnvironment(
            testDirectory.Path,
            summaryPath,
            "check",
            "--directory", testDirectory.Path,
            "--glob-patterns", "coverage.xml",
            "--format", "Cobertura",
            "--line-threshold", "100");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("::warning"));
            Assert.That(stdout, Does.Contain("file=src/Foo.cs"));
            Assert.That(stdout, Does.Contain("line=11,endLine=12"));
            Assert.That(stdout, Does.Contain("title=Missing Line Coverage"));
        }
    }

    [Test]
    public async Task CheckCommandEmitsGitHubAnnotationForPartialBranchWhenThresholdFails()
    {
        using TestDirectory testDirectory = new();
        string coverageXml = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <coverage>
              <sources>
                <source>{testDirectory.Path}</source>
              </sources>
              <packages>
                <package name="package-1">
                  <classes>
                    <class name="class-1" filename="src/Branchy.cs">
                      <methods/>
                      <lines>
                        <line number="20" hits="1" branch="True" condition-coverage="50% (1/2)"/>
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;
        File.WriteAllText(Path.Combine(testDirectory.Path, "coverage.xml"), coverageXml);
        string summaryPath = Path.Combine(testDirectory.Path, "summary.md");

        (int exitCode, string stdout) = await RunCliWithGitHubEnvironment(
            testDirectory.Path,
            summaryPath,
            "check",
            "--directory", testDirectory.Path,
            "--glob-patterns", "coverage.xml",
            "--format", "Cobertura",
            "--branch-threshold", "100");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("::warning"));
            Assert.That(stdout, Does.Contain("file=src/Branchy.cs"));
            Assert.That(stdout, Does.Contain("line=20"));
            Assert.That(stdout, Does.Contain("title=Partial Branch Coverage"));
            Assert.That(stdout, Does.Contain("1 / 2 branches covered."));
        }
    }

    [Test]
    public async Task CheckCommandEscapesGitHubAnnotationPathProperties()
    {
        using TestDirectory testDirectory = new();
        File.WriteAllText(Path.Combine(testDirectory.Path, "coverage.xml"), CreateCoverageXml(testDirectory.Path, "src/Comma,File.cs", (5, 0)));
        string summaryPath = Path.Combine(testDirectory.Path, "summary.md");

        (int exitCode, string stdout) = await RunCliWithGitHubEnvironment(
            testDirectory.Path,
            summaryPath,
            "check",
            "--directory", testDirectory.Path,
            "--glob-patterns", "coverage.xml",
            "--format", "Cobertura",
            "--line-threshold", "100");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("::warning"));
            Assert.That(stdout, Does.Contain("file=src/Comma%2CFile.cs"));
            Assert.That(stdout, Does.Contain("[src/Comma,File.cs : 5]"));
        }
    }

    [Test]
    public async Task CheckCommandDoesNotEmitGitHubAnnotationsWhenThresholdPasses()
    {
        using TestDirectory testDirectory = new();
        File.WriteAllText(Path.Combine(testDirectory.Path, "coverage.xml"), CreateCoverageXml(testDirectory.Path, "src/Foo.cs", (10, 1), (11, 0), (12, 1)));
        string summaryPath = Path.Combine(testDirectory.Path, "summary.md");

        (int exitCode, string stdout) = await RunCliWithGitHubEnvironment(
            testDirectory.Path,
            summaryPath,
            "check",
            "--directory", testDirectory.Path,
            "--glob-patterns", "coverage.xml",
            "--format", "Cobertura",
            "--line-threshold", "50");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Zero);
            Assert.That(stdout, Does.Not.Contain("::warning"));
            Assert.That(stdout, Does.Not.Contain("title=Missing Line Coverage"));
            Assert.That(stdout, Does.Not.Contain("title=Partial Branch Coverage"));
        }
    }

}
