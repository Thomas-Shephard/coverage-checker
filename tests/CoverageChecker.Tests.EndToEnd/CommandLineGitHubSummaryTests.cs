namespace CoverageChecker.Tests.EndToEnd;

internal sealed class CommandLineGitHubSummaryTests : CommandLineTestBase
{
    [Test]
    public async Task CheckCommandWritesGitHubSummaryForPassingCoverage()
    {
        using TestDirectory testDirectory = new();
        File.Copy(Path.Combine(CoberturaCoverageFiles, "FullBranchCoverage.xml"), Path.Combine(testDirectory.Path, "coverage.xml"));
        string summaryPath = Path.Combine(testDirectory.Path, "summary.md");

        (int exitCode, string stdout) = await RunCliWithGitHubEnvironment(
            testDirectory.Path,
            summaryPath,
            "check",
            "--directory", testDirectory.Path,
            "--glob-patterns", "coverage.xml",
            "--format", "Cobertura",
            "--line-threshold", "100",
            "--branch-threshold", "100");

        string summary = await File.ReadAllTextAsync(summaryPath);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Zero);
            Assert.That(stdout, Does.Not.Contain("::error::"));
            Assert.That(summary, Does.Contain("| **Line Coverage** | 100.00 % | 100.00 % | ✅ |"));
            Assert.That(summary, Does.Contain("| **Branch Coverage** | 100.00 % | 100.00 % | ✅ |"));
        }
    }

    [Test]
    public async Task CheckCommandWritesGitHubSummaryAndWorkflowCommandForFailingThreshold()
    {
        using TestDirectory testDirectory = new();
        File.Copy(Path.Combine(CoberturaCoverageFiles, "PartialLineCoverage.xml"), Path.Combine(testDirectory.Path, "coverage.xml"));
        string summaryPath = Path.Combine(testDirectory.Path, "summary.md");

        (int exitCode, string stdout) = await RunCliWithGitHubEnvironment(
            testDirectory.Path,
            summaryPath,
            "check",
            "--directory", testDirectory.Path,
            "--glob-patterns", "coverage.xml",
            "--format", "Cobertura",
            "--line-threshold", "100");

        string summary = await File.ReadAllTextAsync(summaryPath);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(summary, Does.Contain("| **Line Coverage** | 20.00 % | 100.00 % | ❌ |"));
            Assert.That(stdout, Does.Contain("::error::"));
            Assert.That(stdout, Does.Contain("Line coverage of 20.00 %25 is below the required threshold of 100.00 %25"));
        }
    }


    [Test]
    public async Task CheckCommandWritesGitHubSummaryWithEffectiveDeltaThresholds()
    {
        using TestDirectory testDirectory = new();
        string baseCommit = CreateGitRepoWithInitialCommit(testDirectory.Path, ("Changed.cs", CreateChangedClass("1")));

        WriteTextFile(testDirectory.Path, "Changed.cs", CreateChangedClass("2"));
        RunGit(testDirectory.Path, "add Changed.cs");
        RunGit(testDirectory.Path, "commit -m \"Update changed line\"");
        File.WriteAllText(Path.Combine(testDirectory.Path, "coverage.xml"), CreateCoverageXml(testDirectory.Path, "Changed.cs", (1, 1), (2, 1), (3, 0), (4, 1), (5, 1)));
        string summaryPath = Path.Combine(testDirectory.Path, "summary.md");

        (int exitCode, string stdout) = await RunCliWithGitHubEnvironment(
            testDirectory.Path,
            summaryPath,
            "check",
            "--directory", testDirectory.Path,
            "--glob-patterns", "coverage.xml",
            "--format", "Cobertura",
            "--delta",
            "--delta-base", baseCommit,
            "--line-threshold", "80",
            "--delta-line-threshold", "100");

        string summary = await File.ReadAllTextAsync(summaryPath);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("::error::"));
            Assert.That(summary, Does.Contain("| **Line Coverage** | 80.00 % | 80.00 % | ✅ |"));
            Assert.That(summary, Does.Contain("| **Delta Line Coverage** | 0.00 % | 100.00 % | ❌ |"));
            Assert.That(summary, Does.Not.Contain("| **Delta Line Coverage** | 0.00 % | 80.00 % |"));
        }
    }

    [Test]
    public async Task CheckCommandWritesGitHubSummaryForStrictDeltaMissingFileFailure()
    {
        using TestDirectory testDirectory = new();
        string baseCommit = CreateGitRepoWithInitialCommit(testDirectory.Path, ("Missing.cs", "public class Missing\n{\n}\n"));

        WriteTextFile(testDirectory.Path, "Missing.cs", "public class Missing\n{\n    public void Method() {}\n}\n");
        RunGit(testDirectory.Path, "add Missing.cs");
        RunGit(testDirectory.Path, "commit -m \"Update missing source\"");

        WriteTextFile(testDirectory.Path, "Covered.cs", "public class Covered {}\n");
        File.WriteAllText(Path.Combine(testDirectory.Path, "coverage.xml"), CreateCoverageXml(testDirectory.Path, "Covered.cs"));
        string summaryPath = Path.Combine(testDirectory.Path, "summary.md");

        (int exitCode, string stdout) = await RunCliWithGitHubEnvironment(
            testDirectory.Path,
            summaryPath,
            "check",
            "--directory", testDirectory.Path,
            "--glob-patterns", "coverage.xml",
            "--format", "Cobertura",
            "--delta",
            "--strict-delta",
            "--delta-base", baseCommit);

        string summary = await File.ReadAllTextAsync(summaryPath);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("::error::"));
            Assert.That(summary, Does.Contain("| **Delta Coverage** | N/A (Changed files missing from coverage data) | - | ❌ |"));
            Assert.That(summary, Does.Contain("| **Line Coverage** | 100.00 % | 80.00 % | ✅ |"));
        }
    }

    [Test]
    public async Task CheckCommandWritesGitHubSummaryForNoApplicableLineCoverage()
    {
        using TestDirectory testDirectory = new();
        File.Copy(Path.Combine(CoberturaCoverageFiles, "NoLines.xml"), Path.Combine(testDirectory.Path, "coverage.xml"));
        string summaryPath = Path.Combine(testDirectory.Path, "summary.md");

        (int exitCode, string stdout) = await RunCliWithGitHubEnvironment(
            testDirectory.Path,
            summaryPath,
            "check",
            "--directory", testDirectory.Path,
            "--glob-patterns", "coverage.xml",
            "--format", "Cobertura");

        string summary = await File.ReadAllTextAsync(summaryPath);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("::error::"));
            Assert.That(summary, Does.Contain("| **Line Coverage** | N/A | 80.00 % | ❌ |"));
        }
    }

}
