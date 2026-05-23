using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CoverageChecker.Tests.EndToEnd;

public class CommandLineRunTests
{
    private static readonly string CoberturaCoverageFiles = Path.Combine(TestContext.CurrentContext.TestDirectory, "CoverageFiles", "Cobertura");

    private static string GetCliPath()
    {
        string? envPath = Environment.GetEnvironmentVariable("COVERAGE_CHECKER_CLI_PATH");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
            return envPath;

        string baseDir = AppContext.BaseDirectory;
        DirectoryInfo? dir = new(baseDir);

        // Find the project root by looking for the solution or artifacts folder
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "artifacts")))
        {
            dir = dir.Parent;
        }

        if (dir == null)
            throw new InvalidOperationException("Could not find artifacts folder");

        string artifactsDir = Path.Combine(dir.FullName, "artifacts", "bin", "CoverageChecker.CommandLine");
        string exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "CoverageChecker.CommandLine.exe" : "CoverageChecker.CommandLine";

        // Try to find the executable in a directory that matches the current configuration (Debug/Release)
        // baseDir example: .../bin/Debug/net8.0
        string configName = new DirectoryInfo(baseDir).Parent?.Name ?? "Debug";
        string? exePath = Directory.GetFiles(artifactsDir, exeName, SearchOption.AllDirectories)
                                   .FirstOrDefault(f => f.Contains(configName) && !f.Contains("publish"));

        // Fallback to any match if configuration-specific one isn't found
        exePath ??= Directory.GetFiles(artifactsDir, exeName, SearchOption.AllDirectories)
                                .FirstOrDefault(f => !f.Contains("publish"));

        return exePath ?? throw new InvalidOperationException($"Could not find {exeName} executable in {artifactsDir}. Set COVERAGE_CHECKER_CLI_PATH to override.");
    }

    [Test]
    public async Task RunCommandExecutesAndAttemptsAnalysis()
    {
        string cliPath = GetCliPath();
        
        string command = "echo dummy > {output}/coverage.xml";
        
        ProcessStartInfo psi = new()
        {
            FileName = cliPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        RemoveGitHubActionsEnvironment(psi);

        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--command");
        psi.ArgumentList.Add(command);
        psi.ArgumentList.Add("--continue-on-failure");
        
        using Process? process = Process.Start(psi);
        Assert.That(process, Is.Not.Null);
        
        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        string stdout = await stdoutTask;
        await stderrTask;
        
        // We expect it to find the file but fail to parse it
        // Note: With log level Warning, we won't see "Found 1 coverage files" in stdout
        // but we will see the failure message.
        Assert.That(stdout, Does.Contain("Error parsing coverage files."));
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
        string cliPath = GetCliPath();
        
        // Command that doesn't create any files
        string command = "echo hello";
        
        ProcessStartInfo psi = new()
        {
            FileName = cliPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        RemoveGitHubActionsEnvironment(psi);

        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--command");
        psi.ArgumentList.Add(command);
        psi.ArgumentList.Add("--continue-on-failure");
        
        using Process? process = Process.Start(psi);
        Assert.That(process, Is.Not.Null);
        
        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        string stdout = await stdoutTask;
        await stderrTask;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(process.ExitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("No coverage files found."));
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

    [Test]
    public async Task CheckCommandFailsInStrictDeltaWhenChangedSourceFileIsAbsentFromCoverageFiles()
    {
        using TestDirectory testDirectory = new();
        RunGit(testDirectory.Path, "init");
        RunGit(testDirectory.Path, "config user.email \"test@example.com\"");
        RunGit(testDirectory.Path, "config user.name \"Test User\"");
        RunGit(testDirectory.Path, "config commit.gpgsign false");
        RunGit(testDirectory.Path, "config core.autocrlf false");

        string missingFile = Path.Combine(testDirectory.Path, "Missing.cs");
        File.WriteAllText(missingFile, "public class Missing\n{\n}\n");
        RunGit(testDirectory.Path, "add Missing.cs");
        RunGit(testDirectory.Path, "commit -m \"Initial\"");
        string baseCommit = RunGit(testDirectory.Path, "rev-parse HEAD").Trim();

        File.WriteAllText(missingFile, "public class Missing\n{\n    public void Method() {}\n}\n");
        RunGit(testDirectory.Path, "add Missing.cs");
        RunGit(testDirectory.Path, "commit -m \"Update missing source\"");

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

        (int exitCode, string stdout) = await RunCliInDirectory(testDirectory.Path, "check", "--directory", testDirectory.Path, "--glob-patterns", "coverage.xml", "--format", "Cobertura", "--delta", "--strict-delta", "--delta-base", baseCommit);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("Strict delta coverage failed because 1 changed file(s) were absent from coverage data:"));
            Assert.That(stdout, Does.Contain("Missing.cs"));
        }
    }

    [Test]
    public async Task CheckCommandIgnoresStrictDeltaMissingFileOutsideIncludeScope()
    {
        using TestDirectory testDirectory = new();
        string baseCommit = CreateGitRepoWithInitialCommit(testDirectory.Path, ("README.md", "# Project\n"));

        WriteTextFile(testDirectory.Path, "README.md", "# Project\n\nDocs update.\n");
        RunGit(testDirectory.Path, "add README.md");
        RunGit(testDirectory.Path, "commit -m \"Update docs\"");

        WriteTextFile(testDirectory.Path, "src/Covered.cs", "public class Covered {}\n");
        File.WriteAllText(Path.Combine(testDirectory.Path, "coverage.xml"), CreateCoverageXml(testDirectory.Path, "src/Covered.cs"));

        (int exitCode, string stdout) = await RunCliInDirectory(
            testDirectory.Path,
            "check",
            "--directory", testDirectory.Path,
            "--glob-patterns", "coverage.xml",
            "--format", "Cobertura",
            "--delta",
            "--strict-delta",
            "--include", "src/**/*.cs",
            "--delta-base", baseCommit);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Zero);
            Assert.That(stdout, Does.Not.Contain("Strict delta coverage failed"));
        }
    }

    [Test]
    public async Task CheckCommandFailsStrictDeltaForMissingFileInsideIncludeScope()
    {
        using TestDirectory testDirectory = new();
        string baseCommit = CreateGitRepoWithInitialCommit(testDirectory.Path, ("src/Foo.cs", "public class Foo\n{\n}\n"));

        WriteTextFile(testDirectory.Path, "src/Foo.cs", "public class Foo\n{\n    public void Method() {}\n}\n");
        RunGit(testDirectory.Path, "add src/Foo.cs");
        RunGit(testDirectory.Path, "commit -m \"Update source\"");

        WriteTextFile(testDirectory.Path, "src/Covered.cs", "public class Covered {}\n");
        File.WriteAllText(Path.Combine(testDirectory.Path, "coverage.xml"), CreateCoverageXml(testDirectory.Path, "src/Covered.cs"));

        (int exitCode, string stdout) = await RunCliInDirectory(
            testDirectory.Path,
            "check",
            "--directory", testDirectory.Path,
            "--glob-patterns", "coverage.xml",
            "--format", "Cobertura",
            "--delta",
            "--strict-delta",
            "--include", "src/**/*.cs",
            "--delta-base", baseCommit);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("Strict delta coverage failed because 1 changed file(s) were absent from coverage data:"));
            Assert.That(stdout, Does.Contain("src/Foo.cs"));
        }
    }

    [Test]
    public async Task CheckCommandUsesSameFilterRootForStrictDeltaWhenRunFromSubdirectory()
    {
        using TestDirectory testDirectory = new();
        string baseCommit = CreateGitRepoWithInitialCommit(testDirectory.Path, ("src/Foo.cs", "public class Foo\n{\n}\n"));
        string workingDirectory = Path.Combine(testDirectory.Path, "tools");
        Directory.CreateDirectory(workingDirectory);

        WriteTextFile(testDirectory.Path, "src/Foo.cs", "public class Foo\n{\n    public void Method() {}\n}\n");
        RunGit(testDirectory.Path, "add src/Foo.cs");
        RunGit(testDirectory.Path, "commit -m \"Update source\"");

        WriteTextFile(testDirectory.Path, "src/Covered.cs", "public class Covered {}\n");
        File.WriteAllText(Path.Combine(testDirectory.Path, "coverage.xml"), CreateCoverageXml(testDirectory.Path, "src/Covered.cs"));

        (int exitCode, string stdout) = await RunCliInDirectory(
            workingDirectory,
            "check",
            "--directory", testDirectory.Path,
            "--glob-patterns", "coverage.xml",
            "--format", "Cobertura",
            "--delta",
            "--strict-delta",
            "--include", "src/**/*.cs",
            "--delta-base", baseCommit);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("Strict delta coverage failed because 1 changed file(s) were absent from coverage data:"));
            Assert.That(stdout, Does.Contain("src/Foo.cs"));
        }
    }

    [Test]
    public async Task CheckCommandIgnoresStrictDeltaMissingFileInsideExcludeScope()
    {
        using TestDirectory testDirectory = new();
        string baseCommit = CreateGitRepoWithInitialCommit(
            testDirectory.Path,
            ("src/Foo.Generated.cs", "public class FooGenerated\n{\n}\n"),
            ("src/Bar.cs", "public class Bar\n{\n}\n"));

        WriteTextFile(testDirectory.Path, "src/Foo.Generated.cs", "public class FooGenerated\n{\n    public void Method() {}\n}\n");
        WriteTextFile(testDirectory.Path, "src/Bar.cs", "public class Bar\n{\n    public void Method() {}\n}\n");
        RunGit(testDirectory.Path, "add src/Foo.Generated.cs src/Bar.cs");
        RunGit(testDirectory.Path, "commit -m \"Update source\"");

        WriteTextFile(testDirectory.Path, "src/Covered.cs", "public class Covered {}\n");
        File.WriteAllText(Path.Combine(testDirectory.Path, "coverage.xml"), CreateCoverageXml(testDirectory.Path, "src/Covered.cs"));

        (int exitCode, string stdout) = await RunCliInDirectory(
            testDirectory.Path,
            "check",
            "--directory", testDirectory.Path,
            "--glob-patterns", "coverage.xml",
            "--format", "Cobertura",
            "--delta",
            "--strict-delta",
            "--exclude", "**/*.Generated.cs",
            "--delta-base", baseCommit);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("Strict delta coverage failed because 1 changed file(s) were absent from coverage data:"));
            Assert.That(stdout, Does.Contain("src/Bar.cs"));
            Assert.That(stdout, Does.Not.Contain("Foo.Generated.cs"));
        }
    }

    [Test]
    public async Task CheckCommandFailsInStrictDeltaWhenOneChangedFileMatchesCoverageAndAnotherIsMissing()
    {
        using TestDirectory testDirectory = new();
        RunGit(testDirectory.Path, "init");
        RunGit(testDirectory.Path, "config user.email \"test@example.com\"");
        RunGit(testDirectory.Path, "config user.name \"Test User\"");
        RunGit(testDirectory.Path, "config commit.gpgsign false");
        RunGit(testDirectory.Path, "config core.autocrlf false");

        string coveredFile = Path.Combine(testDirectory.Path, "Covered.cs");
        string missingFile = Path.Combine(testDirectory.Path, "Missing.cs");
        File.WriteAllText(coveredFile, "public class Covered\n{\n}\n");
        File.WriteAllText(missingFile, "public class Missing\n{\n}\n");
        RunGit(testDirectory.Path, "add .");
        RunGit(testDirectory.Path, "commit -m \"Initial\"");
        string baseCommit = RunGit(testDirectory.Path, "rev-parse HEAD").Trim();

        File.WriteAllText(coveredFile, "public class Covered\n{\n    public void CoveredMethod() {}\n}\n");
        File.WriteAllText(missingFile, "public class Missing\n{\n    public void MissingMethod() {}\n}\n");
        RunGit(testDirectory.Path, "add .");
        RunGit(testDirectory.Path, "commit -m \"Update source\"");

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
                        <line number="3" hits="1"/>
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;
        File.WriteAllText(Path.Combine(testDirectory.Path, "coverage.xml"), coverageXml);

        (int exitCode, string stdout) = await RunCliInDirectory(testDirectory.Path, "check", "--directory", testDirectory.Path, "--glob-patterns", "coverage.xml", "--format", "Cobertura", "--delta", "--strict-delta", "--delta-base", baseCommit);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("Delta line coverage: 100.00"));
            Assert.That(stdout, Does.Contain("Strict delta coverage failed because 1 changed file(s) were absent from coverage data:"));
            Assert.That(stdout, Does.Contain("Missing.cs"));
        }
    }

    private static async Task<(int ExitCode, string Stdout)> RunCli(params string[] arguments)
    {
        return await RunCliInDirectory(Environment.CurrentDirectory, arguments);
    }

    private static async Task<(int ExitCode, string Stdout)> RunCliInDirectory(string workingDirectory, params string[] arguments)
    {
        return await RunCliInDirectory(workingDirectory, environment: null, arguments);
    }

    private static async Task<(int ExitCode, string Stdout)> RunCliWithGitHubEnvironment(string workingDirectory, string summaryPath, params string[] arguments)
    {
        return await RunCliInDirectory(
            workingDirectory,
            new Dictionary<string, string?>
            {
                ["GITHUB_ACTIONS"] = "true",
                ["GITHUB_STEP_SUMMARY"] = summaryPath,
                ["GITHUB_WORKSPACE"] = workingDirectory
            },
            arguments);
    }

    private static async Task<(int ExitCode, string Stdout)> RunCliInDirectory(string workingDirectory, IReadOnlyDictionary<string, string?>? environment, params string[] arguments)
    {
        ProcessStartInfo psi = new()
        {
            FileName = GetCliPath(),
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        RemoveGitHubActionsEnvironment(psi);
        ApplyEnvironment(psi, environment);

        foreach (string argument in arguments)
        {
            psi.ArgumentList.Add(argument);
        }

        using Process? process = Process.Start(psi);
        Assert.That(process, Is.Not.Null);

        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        string stdout = await stdoutTask;
        string stderr = await stderrTask;

        return (process.ExitCode, stdout + stderr);
    }

    private static void RemoveGitHubActionsEnvironment(ProcessStartInfo psi)
    {
        psi.Environment.Remove("GITHUB_ACTIONS");
        psi.Environment.Remove("GITHUB_STEP_SUMMARY");
        psi.Environment.Remove("GITHUB_WORKSPACE");
    }

    private static void ApplyEnvironment(ProcessStartInfo psi, IReadOnlyDictionary<string, string?>? environment)
    {
        if (environment is null) return;

        foreach ((string key, string? value) in environment)
        {
            if (value is null)
            {
                psi.Environment.Remove(key);
            }
            else
            {
                psi.Environment[key] = value;
            }
        }
    }

    private static string RunGit(string workingDirectory, string arguments)
    {
        ProcessStartInfo psi = new()
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process? process = Process.Start(psi);
        Assert.That(process, Is.Not.Null);
        process.WaitForExit();
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        Assert.That(process.ExitCode, Is.Zero, stderr);
        return stdout;
    }

    private static string CreateGitRepoWithInitialCommit(string workingDirectory, params (string RelativePath, string Contents)[] files)
    {
        RunGit(workingDirectory, "init");
        RunGit(workingDirectory, "config user.email \"test@example.com\"");
        RunGit(workingDirectory, "config user.name \"Test User\"");
        RunGit(workingDirectory, "config commit.gpgsign false");
        RunGit(workingDirectory, "config core.autocrlf false");

        foreach ((string relativePath, string contents) in files)
        {
            WriteTextFile(workingDirectory, relativePath, contents);
        }

        RunGit(workingDirectory, "add .");
        RunGit(workingDirectory, "commit -m \"Initial\"");
        return RunGit(workingDirectory, "rev-parse HEAD").Trim();
    }

    private static void WriteTextFile(string rootDirectory, string relativePath, string contents)
    {
        string path = Path.Combine(rootDirectory, relativePath);
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, contents);
    }

    private static string CreateCoverageXml(string sourceDirectory, string fileName)
    {
        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <coverage>
              <sources>
                <source>{sourceDirectory}</source>
              </sources>
              <packages>
                <package name="package-1">
                  <classes>
                    <class name="class-1" filename="{fileName}">
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
    }

    private static string CreateCoverageXml(string sourceDirectory, string fileName, params (int Number, int Hits)[] lines)
    {
        string lineElements = string.Join(
            Environment.NewLine,
            lines.Select(line => $"                        <line number=\"{line.Number}\" hits=\"{line.Hits}\"/>"));

        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <coverage>
              <sources>
                <source>{sourceDirectory}</source>
              </sources>
              <packages>
                <package name="package-1">
                  <classes>
                    <class name="class-1" filename="{fileName}">
                      <methods/>
                      <lines>
            {lineElements}
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;
    }

    private static string CreateChangedClass(string value)
    {
        return string.Join(
            Environment.NewLine,
            "public class Changed",
            "{",
            $"    public int Value => {value};",
            "}",
            string.Empty);
    }

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
            System.IO.Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (System.IO.Directory.Exists(Path))
                {
                    foreach (string file in System.IO.Directory.GetFiles(Path, "*", SearchOption.AllDirectories))
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                    }

                    System.IO.Directory.Delete(Path, true);
                }
            }
            catch
            {
                // Ignore cleanup failures from transient file locks.
            }
        }
    }
}
