namespace CoverageChecker.Tests.EndToEnd;

internal sealed class CommandLineDeltaTests : CommandLineTestBase
{
    [Test]
    public async Task CheckCommandFailsWhenDeltaChangedLinesAreMissingFromCoverage()
    {
        using TestDirectory testDirectory = new();
        RunGit(testDirectory.Path, "init");
        RunGit(testDirectory.Path, "config user.email \"test@example.com\"");
        RunGit(testDirectory.Path, "config user.name \"Test User\"");
        RunGit(testDirectory.Path, "config commit.gpgsign false");
        RunGit(testDirectory.Path, "config core.autocrlf false");

        string changedFile = Path.Combine(testDirectory.Path, "Changed.cs");
        File.WriteAllText(changedFile, "public class Changed\n{\n}\n");
        RunGit(testDirectory.Path, "add .");
        RunGit(testDirectory.Path, "commit -m \"Initial\"");
        string baseCommit = RunGit(testDirectory.Path, "rev-parse HEAD").Trim();

        File.WriteAllText(changedFile, "public class Changed\n{\n    public void Method() {}\n}\n");
        RunGit(testDirectory.Path, "add Changed.cs");
        RunGit(testDirectory.Path, "commit -m \"Update changed file\"");

        string coverageXml = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <coverage>
              <sources>
                <source>{testDirectory.Path}</source>
              </sources>
              <packages>
                <package name="package-1">
                  <classes>
                    <class name="class-1" filename="Changed.cs">
                      <methods/>
                      <lines>
                        <line number="1" hits="1"/>
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;
        File.WriteAllText(Path.Combine(testDirectory.Path, "coverage.xml"), coverageXml);

        (int exitCode, string stdout) = await RunCliInDirectory(testDirectory.Path, "check", "--directory", testDirectory.Path, "--glob-patterns", "coverage.xml", "--format", "Cobertura", "--delta", "--delta-base", baseCommit);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("Git reported changed lines, but none were found in the coverage data."));
            Assert.That(stdout, Does.Not.Contain("No changed lines found for delta coverage."));
        }
    }

    [Test]
    public async Task CheckCommandFailsDeltaLineThresholdIndependentlyFromOverallThreshold()
    {
        using TestDirectory testDirectory = new();
        string baseCommit = CreateGitRepoWithInitialCommit(testDirectory.Path, ("Changed.cs", CreateChangedClass("1")));

        WriteTextFile(testDirectory.Path, "Changed.cs", CreateChangedClass("2"));
        RunGit(testDirectory.Path, "add Changed.cs");
        RunGit(testDirectory.Path, "commit -m \"Update changed line\"");

        File.WriteAllText(Path.Combine(testDirectory.Path, "coverage.xml"), CreateCoverageXml(testDirectory.Path, "Changed.cs", (1, 1), (2, 1), (3, 0), (4, 1), (5, 1)));

        (int exitCode, string stdout) = await RunCliInDirectory(
            testDirectory.Path,
            "check",
            "--directory", testDirectory.Path,
            "--glob-patterns", "coverage.xml",
            "--format", "Cobertura",
            "--delta",
            "--delta-base", baseCommit,
            "--line-threshold", "80",
            "--delta-line-threshold", "100");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("Overall line coverage: 80.00"));
            Assert.That(stdout, Does.Contain("Delta line coverage: 0.00"));
            Assert.That(stdout, Does.Contain("Delta line coverage of 0.00 % is below the required threshold of 100.00 %"));
            Assert.That(stdout, Does.Not.Contain("Line coverage of 80.00 % is below"));
        }
    }

    [Test]
    public async Task CheckCommandFailsOverallThresholdIndependentlyFromLowerDeltaThreshold()
    {
        using TestDirectory testDirectory = new();
        string baseCommit = CreateGitRepoWithInitialCommit(testDirectory.Path, ("Changed.cs", CreateChangedClass("1")));

        WriteTextFile(testDirectory.Path, "Changed.cs", CreateChangedClass("2"));
        RunGit(testDirectory.Path, "add Changed.cs");
        RunGit(testDirectory.Path, "commit -m \"Update changed line\"");

        File.WriteAllText(Path.Combine(testDirectory.Path, "coverage.xml"), CreateCoverageXml(testDirectory.Path, "Changed.cs", (1, 1), (2, 1), (3, 1), (4, 0), (5, 0)));

        (int exitCode, string stdout) = await RunCliInDirectory(
            testDirectory.Path,
            "check",
            "--directory", testDirectory.Path,
            "--glob-patterns", "coverage.xml",
            "--format", "Cobertura",
            "--delta",
            "--delta-base", baseCommit,
            "--line-threshold", "80",
            "--delta-line-threshold", "0");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("Line coverage of 60.00 % is below the required threshold of 80.00 %"));
            Assert.That(stdout, Does.Contain("Delta line coverage: 100.00"));
            Assert.That(stdout, Does.Not.Contain("Delta line coverage of 100.00 % is below"));
        }
    }

    [Test]
    public async Task CheckCommandDefaultsDeltaThresholdsToOverallThresholds()
    {
        using TestDirectory testDirectory = new();
        string baseCommit = CreateGitRepoWithInitialCommit(testDirectory.Path, ("Changed.cs", CreateChangedClass("1")));

        WriteTextFile(testDirectory.Path, "Changed.cs", CreateChangedClass("2"));
        RunGit(testDirectory.Path, "add Changed.cs");
        RunGit(testDirectory.Path, "commit -m \"Update changed line\"");

        File.WriteAllText(Path.Combine(testDirectory.Path, "coverage.xml"), CreateCoverageXml(testDirectory.Path, "Changed.cs", (1, 1), (2, 1), (3, 0), (4, 1), (5, 1)));

        (int exitCode, string stdout) = await RunCliInDirectory(
            testDirectory.Path,
            "check",
            "--directory", testDirectory.Path,
            "--glob-patterns", "coverage.xml",
            "--format", "Cobertura",
            "--delta",
            "--delta-base", baseCommit,
            "--line-threshold", "80");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("Delta line coverage of 0.00 % is below the required threshold of 80.00 %"));
        }
    }

    [Test]
    public async Task CheckCommandUsesDeltaBranchThreshold()
    {
        using TestDirectory testDirectory = new();
        string baseCommit = CreateGitRepoWithInitialCommit(testDirectory.Path, ("Changed.cs", CreateChangedClass("1")));

        WriteTextFile(testDirectory.Path, "Changed.cs", CreateChangedClass("2"));
        RunGit(testDirectory.Path, "add Changed.cs");
        RunGit(testDirectory.Path, "commit -m \"Update changed line\"");

        string coverageXml = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <coverage>
              <sources>
                <source>{testDirectory.Path}</source>
              </sources>
              <packages>
                <package name="package-1">
                  <classes>
                    <class name="class-1" filename="Changed.cs">
                      <methods/>
                      <lines>
                        <line number="1" hits="1"/>
                        <line number="2" hits="1"/>
                        <line number="3" hits="1" branch="True" condition-coverage="50% (1/2)"/>
                        <line number="4" hits="1"/>
                        <line number="5" hits="1"/>
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;
        File.WriteAllText(Path.Combine(testDirectory.Path, "coverage.xml"), coverageXml);

        (int exitCode, string stdout) = await RunCliInDirectory(
            testDirectory.Path,
            "check",
            "--directory", testDirectory.Path,
            "--glob-patterns", "coverage.xml",
            "--format", "Cobertura",
            "--delta",
            "--delta-base", baseCommit,
            "--branch-threshold", "0",
            "--delta-branch-threshold", "100");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("Delta branch coverage: 50.00"));
            Assert.That(stdout, Does.Contain("Delta branch coverage of 50.00 % is below the required threshold of 100.00 %"));
            Assert.That(stdout, Does.Not.Contain("Branch coverage of 50.00 % is below"));
        }
    }

    [TestCase("--delta-line-threshold", "-1")]
    [TestCase("--delta-line-threshold", "101")]
    [TestCase("--delta-line-threshold", "NaN")]
    [TestCase("--delta-branch-threshold", "-1")]
    [TestCase("--delta-branch-threshold", "101")]
    [TestCase("--delta-branch-threshold", "NaN")]
    public async Task CheckCommandFailsParsingForInvalidDeltaThresholdValues(string option, string value)
    {
        using TestDirectory testDirectory = new();

        (int exitCode, string stdout) = await RunCli(
            "check",
            "--directory", testDirectory.Path,
            option, value);

        Assert.That(exitCode, Is.Not.Zero);
    }

    [Test]
    public async Task CheckCommandPassesWhenOnlyDeltaChangesAreAbsentFromCoverageFiles()
    {
        using TestDirectory testDirectory = new();
        RunGit(testDirectory.Path, "init");
        RunGit(testDirectory.Path, "config user.email \"test@example.com\"");
        RunGit(testDirectory.Path, "config user.name \"Test User\"");
        RunGit(testDirectory.Path, "config commit.gpgsign false");
        RunGit(testDirectory.Path, "config core.autocrlf false");

        string readme = Path.Combine(testDirectory.Path, "README.md");
        File.WriteAllText(readme, "# Project\n");
        RunGit(testDirectory.Path, "add README.md");
        RunGit(testDirectory.Path, "commit -m \"Initial\"");
        string baseCommit = RunGit(testDirectory.Path, "rev-parse HEAD").Trim();

        File.WriteAllText(readme, "# Project\n\nDocs update.\n");
        RunGit(testDirectory.Path, "add README.md");
        RunGit(testDirectory.Path, "commit -m \"Update docs\"");

        string coverageXml = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <coverage>
              <sources>
                <source>{testDirectory.Path}</source>
              </sources>
              <packages>
                <package name="package-1">
                  <classes>
                    <class name="class-1" filename="Covered.cs">
                      <methods/>
                      <lines>
                        <line number="1" hits="1"/>
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;
        File.WriteAllText(Path.Combine(testDirectory.Path, "Covered.cs"), "public class Covered {}\n");
        File.WriteAllText(Path.Combine(testDirectory.Path, "coverage.xml"), coverageXml);

        (int exitCode, string stdout) = await RunCliInDirectory(testDirectory.Path, "check", "--directory", testDirectory.Path, "--glob-patterns", "coverage.xml", "--format", "Cobertura", "--delta", "--delta-base", baseCommit);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Zero);
            Assert.That(stdout, Does.Contain("No changed lines found in coverage data for delta coverage."));
            Assert.That(stdout, Does.Not.Contain("Git reported changed lines, but none were found in the coverage data."));
        }
    }

}
