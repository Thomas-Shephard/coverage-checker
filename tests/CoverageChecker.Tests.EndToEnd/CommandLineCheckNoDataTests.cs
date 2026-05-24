namespace CoverageChecker.Tests.EndToEnd;

internal sealed class CommandLineCheckNoDataTests : CommandLineTestBase
{
    [Test]
    public async Task CheckCommandFailsWhenCoverageFileParsesNoFiles()
    {
        using TestDirectory testDirectory = new();
        File.Copy(Path.Combine(CoberturaCoverageFiles, "NoPackages.xml"), Path.Combine(testDirectory.Path, "coverage.xml"));

        (int exitCode, string stdout) = await RunCli("check", "--directory", testDirectory.Path, "--glob-patterns", "coverage.xml", "--format", "Cobertura");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("Parsed coverage information for 0 files."));
            Assert.That(stdout, Does.Contain("Line coverage could not be calculated because no applicable lines were found."));
        }
    }

    [Test]
    public async Task CheckCommandFailsWhenParsedFilesHaveNoLines()
    {
        using TestDirectory testDirectory = new();
        File.Copy(Path.Combine(CoberturaCoverageFiles, "NoLines.xml"), Path.Combine(testDirectory.Path, "coverage.xml"));

        (int exitCode, string stdout) = await RunCli("check", "--directory", testDirectory.Path, "--glob-patterns", "coverage.xml", "--format", "Cobertura");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("Parsed coverage information for 2 files."));
            Assert.That(stdout, Does.Contain("Line coverage could not be calculated because no applicable lines were found."));
        }
    }

    [Test]
    public async Task CheckCommandFailsWhenFiltersRemoveAllFiles()
    {
        using TestDirectory testDirectory = new();
        File.Copy(Path.Combine(CoberturaCoverageFiles, "FullLineCoverage.xml"), Path.Combine(testDirectory.Path, "coverage.xml"));

        (int exitCode, string stdout) = await RunCli("check", "--directory", testDirectory.Path, "--glob-patterns", "coverage.xml", "--format", "Cobertura", "--include", "does-not-match/**");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("Parsed coverage information for 0 files."));
            Assert.That(stdout, Does.Contain("Line coverage could not be calculated because no applicable lines were found."));
        }
    }

    [Test]
    public async Task CheckCommandPassesWhenLineCoverageMeetsThresholdAndNoBranchesExist()
    {
        using TestDirectory testDirectory = new();
        string coverageXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage>
              <packages>
                <package name="package-1">
                  <classes>
                    <class name="class-1" filename="file-1">
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

        (int exitCode, string stdout) = await RunCli("check", "--directory", testDirectory.Path, "--glob-patterns", "coverage.xml", "--format", "Cobertura", "--line-threshold", "100", "--branch-threshold", "100");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Zero);
            Assert.That(stdout, Does.Contain("Overall branch coverage: NaN."));
            Assert.That(stdout, Does.Contain("The coverage threshold has been met."));
        }
    }

}
