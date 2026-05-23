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

    private static async Task<(int ExitCode, string Stdout)> RunCli(params string[] arguments)
    {
        ProcessStartInfo psi = new()
        {
            FileName = GetCliPath(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

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
            if (System.IO.Directory.Exists(Path))
            {
                System.IO.Directory.Delete(Path, true);
            }
        }
    }
}
