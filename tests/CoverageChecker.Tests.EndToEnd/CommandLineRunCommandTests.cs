namespace CoverageChecker.Tests.EndToEnd;

internal sealed class CommandLineRunCommandTests : CommandLineTestBase
{
    [Test]
    public async Task RunCommandExecutesAndAttemptsAnalysis()
    {
        string command = "echo dummy > {output}/coverage.xml";

        (int exitCode, string stdout) = await RunCli(
            "run",
            "--command", command,
            "--continue-on-failure");
        
        // We expect it to find the file but fail to parse it
        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("Error parsing coverage files."));
        }
    }

    [Test]
    public async Task RunCommandHandlesOutputPathContainingSpaces()
    {
        using TestDirectory testDirectory = new();
        string outputDirectory = Path.Combine(testDirectory.Path, "coverage results");

        (int exitCode, string stdout) = await RunCli(
            "run",
            "--output", outputDirectory,
            "--command", "echo dummy > {output}/coverage.xml",
            "--continue-on-failure");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(File.Exists(Path.Combine(outputDirectory, "coverage.xml")), Is.True);
            Assert.That(stdout, Does.Contain("Error parsing coverage files."));
        }
    }

    [Test]
    public async Task RunCommandHandlesOutputPathContainingParentheses()
    {
        using TestDirectory testDirectory = new();
        string outputDirectory = Path.Combine(testDirectory.Path, "coverage-results (unit)");

        (int exitCode, string stdout) = await RunCli(
            "run",
            "--output", outputDirectory,
            "--command", "echo dummy > {output}/coverage.xml",
            "--continue-on-failure");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(File.Exists(Path.Combine(outputDirectory, "coverage.xml")), Is.True);
            Assert.That(stdout, Does.Contain("Error parsing coverage files."));
        }
    }

    [Test]
    public async Task RunCommandUsesTemporaryOutputWhenOutputIsOmitted()
    {
        using TestDirectory testDirectory = new();

        (int exitCode, string stdout) = await RunCliInDirectory(
            testDirectory.Path,
            "run",
            "--command", "echo dummy > {output}/coverage.xml",
            "--continue-on-failure");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(Directory.GetFiles(testDirectory.Path, "coverage.xml", SearchOption.AllDirectories), Is.Empty);
            Assert.That(stdout, Does.Contain("Error parsing coverage files."));
        }
    }

    [Test]
    public async Task RunCommandFailsWhenNoFilesFound()
    {
        // Command that doesn't create any files
        string command = "echo hello";

        (int exitCode, string stdout) = await RunCli(
            "run",
            "--command", command,
            "--continue-on-failure");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("No coverage files found."));
        }
    }

    [Test]
    public async Task RunCommandFailureWithoutContinueOnFailureReturnsCommandExitCodeAndSkipsAnalysis()
    {
        using TestDirectory testDirectory = new();
        File.Copy(Path.Combine(CoberturaCoverageFiles, "FullLineCoverage.xml"), Path.Combine(testDirectory.Path, "coverage.xml"));

        (int exitCode, string stdout) = await RunCli(
            "run",
            "--output", testDirectory.Path,
            "--command", "exit 7",
            "--format", "Cobertura",
            "--glob-patterns", "coverage.xml",
            "--line-threshold", "100",
            "--branch-threshold", "0");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.EqualTo(7));
            Assert.That(stdout, Does.Contain("Command failed with exit code 7."));
            Assert.That(stdout, Does.Not.Contain("Parsed coverage information"));
        }
    }

    [Test]
    public async Task RunCommandFailureWithContinueOnFailureAttemptsAnalysis()
    {
        using TestDirectory testDirectory = new();
        File.Copy(Path.Combine(CoberturaCoverageFiles, "FullLineCoverage.xml"), Path.Combine(testDirectory.Path, "coverage.xml"));

        (int exitCode, string stdout) = await RunCli(
            "run",
            "--output", testDirectory.Path,
            "--command", "exit 7",
            "--continue-on-failure",
            "--format", "Cobertura",
            "--glob-patterns", "coverage.xml",
            "--line-threshold", "100",
            "--branch-threshold", "0");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Zero);
            Assert.That(stdout, Does.Contain("Command failed with exit code 7. Continuing with coverage analysis as requested."));
            Assert.That(stdout, Does.Contain("Parsed coverage information for 3 files."));
            Assert.That(stdout, Does.Contain("The coverage threshold has been met."));
        }
    }

    [Test]
    public async Task RunCommandWithoutOutputPlaceholderStillUsesConfiguredOutputDirectory()
    {
        using TestDirectory testDirectory = new();
        string outputDirectory = Path.Combine(testDirectory.Path, "coverage results");
        Directory.CreateDirectory(outputDirectory);

        (int exitCode, string stdout) = await RunCliInDirectory(
            outputDirectory,
            "run",
            "--output", outputDirectory,
            "--command", "echo dummy > coverage.xml",
            "--continue-on-failure");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(File.Exists(Path.Combine(outputDirectory, "coverage.xml")), Is.True);
            Assert.That(stdout, Does.Contain("Error parsing coverage files."));
        }
    }

    [Test]
    public async Task RunCommandExecutesInSpecifiedWorkingDirectory()
    {
        using TestDirectory testDirectory = new();
        string commandDirectory = Path.Combine(testDirectory.Path, "project");
        Directory.CreateDirectory(commandDirectory);

        (int exitCode, string stdout) = await RunCli(
            "run",
            "--working-directory", commandDirectory,
            "--command", "echo marker > cwd-marker.txt && echo dummy > {output}/coverage.xml",
            "--continue-on-failure");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(File.Exists(Path.Combine(commandDirectory, "cwd-marker.txt")), Is.True);
            Assert.That(File.Exists(Path.Combine(testDirectory.Path, "cwd-marker.txt")), Is.False);
            Assert.That(stdout, Does.Contain("Error parsing coverage files."));
        }
    }

    [Test]
    public async Task RunCommandHandlesWorkingDirectoryPathContainingSpaces()
    {
        using TestDirectory testDirectory = new();
        string commandDirectory = Path.Combine(testDirectory.Path, "project with spaces");
        Directory.CreateDirectory(commandDirectory);

        (int exitCode, string stdout) = await RunCli(
            "run",
            "--working-directory", commandDirectory,
            "--command", "echo marker > cwd-marker.txt && echo dummy > {output}/coverage.xml",
            "--continue-on-failure");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(File.Exists(Path.Combine(commandDirectory, "cwd-marker.txt")), Is.True);
            Assert.That(File.Exists(Path.Combine(testDirectory.Path, "cwd-marker.txt")), Is.False);
            Assert.That(stdout, Does.Contain("Error parsing coverage files."));
        }
    }

    [Test]
    public async Task RunCommandHandlesRelativeWorkingDirectory()
    {
        using TestDirectory testDirectory = new();
        string commandDirectory = Path.Combine(testDirectory.Path, "project");
        Directory.CreateDirectory(commandDirectory);

        (int exitCode, string stdout) = await RunCliInDirectory(
            testDirectory.Path,
            "run",
            "--working-directory", "project",
            "--command", "echo marker > relative-marker.txt && echo dummy > {output}/coverage.xml",
            "--continue-on-failure");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(File.Exists(Path.Combine(commandDirectory, "relative-marker.txt")), Is.True);
            Assert.That(File.Exists(Path.Combine(testDirectory.Path, "relative-marker.txt")), Is.False);
            Assert.That(stdout, Does.Contain("Error parsing coverage files."));
        }
    }

    [Test]
    public async Task RunCommandUsesRelativeOutputFromInvocationDirectoryWhenWorkingDirectoryIsSpecified()
    {
        using TestDirectory testDirectory = new();
        string commandDirectory = Path.Combine(testDirectory.Path, "project");
        Directory.CreateDirectory(commandDirectory);

        (int exitCode, string stdout) = await RunCliInDirectory(
            testDirectory.Path,
            "run",
            "--working-directory", "project",
            "--output", "coverage-results",
            "--command", "echo dummy > {output}/coverage.xml",
            "--continue-on-failure");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(File.Exists(Path.Combine(testDirectory.Path, "coverage-results", "coverage.xml")), Is.True);
            Assert.That(File.Exists(Path.Combine(commandDirectory, "coverage-results", "coverage.xml")), Is.False);
            Assert.That(stdout, Does.Contain("Error parsing coverage files."));
        }
    }

    [Test]
    public async Task RunCommandFailsClearlyWhenWorkingDirectoryDoesNotExist()
    {
        using TestDirectory testDirectory = new();
        string missingDirectory = Path.Combine(testDirectory.Path, "missing");

        (int exitCode, string stdout) = await RunCli(
            "run",
            "--working-directory", missingDirectory,
            "--command", "echo marker",
            "--continue-on-failure");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("Working directory does not exist:"));
            Assert.That(stdout, Does.Contain(missingDirectory));
        }
    }

    [Test]
    public async Task RunCommandDefaultsToCurrentDirectory()
    {
        using TestDirectory testDirectory = new();

        (int exitCode, string stdout) = await RunCliInDirectory(
            testDirectory.Path,
            "run",
            "--command", "echo marker > default-marker.txt && echo dummy > {output}/coverage.xml",
            "--continue-on-failure");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(File.Exists(Path.Combine(testDirectory.Path, "default-marker.txt")), Is.True);
            Assert.That(stdout, Does.Contain("Error parsing coverage files."));
        }
    }
}
