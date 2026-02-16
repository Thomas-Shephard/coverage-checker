using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CoverageChecker.Tests.EndToEnd;

public class CommandLineRunTests
{
    private static string GetCliPath()
    {
        string baseDir = AppContext.BaseDirectory;
        DirectoryInfo? dir = new DirectoryInfo(baseDir);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "artifacts")))
        {
            dir = dir.Parent;
        }
        
        if (dir == null) throw new InvalidOperationException("Could not find artifacts folder");
        
        string artifactsDir = Path.Combine(dir.FullName, "artifacts", "bin", "CoverageChecker.CommandLine");
        string exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "CoverageChecker.CommandLine.exe" : "CoverageChecker.CommandLine";
        
        string? exePath = Directory.GetFiles(artifactsDir, exeName, SearchOption.AllDirectories)
                                   .FirstOrDefault(f => !f.Contains("publish")); // Avoid publish folders if they exist
        
        return exePath ?? throw new InvalidOperationException($"Could not find {exeName} executable in {artifactsDir}");
    }

    [Test]
    public void RunCommandExecutesAndAttemptsAnalysis()
    {
        string cliPath = GetCliPath();
        
        string command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "echo dummy > {output}\\coverage.xml"
            : "echo dummy > {output}/coverage.xml";
        
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
        
        using Process? process = Process.Start(psi);
        Assert.That(process, Is.Not.Null);
        string stdout = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        
        // We expect it to find the file but fail to parse it
        // Note: With log level Warning, we won't see "Found 1 coverage files" in stdout
        // but we will see the failure message.
        Assert.That(stdout, Does.Contain("Error parsing coverage files."));
    }

    [Test]
    public void RunCommandFailsWhenNoFilesFound()
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
        
        using Process? process = Process.Start(psi);
        Assert.That(process, Is.Not.Null);
        string stdout = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        
        Assert.Multiple(() => {
            Assert.That(process.ExitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("No coverage files found."));
        });
    }
}
