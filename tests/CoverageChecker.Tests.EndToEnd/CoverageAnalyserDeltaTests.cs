using System.Diagnostics;
using CoverageChecker.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoverageChecker.Tests.EndToEnd;

public class CoverageAnalyserDeltaTests
{
    private string _tempDirectory;
    private string _repoRoot;

    [SetUp]
    public void Setup()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDirectory);
        _repoRoot = _tempDirectory;
        
        RunGit("init");
        RunGit("config user.email \"test@example.com\"");
        RunGit("config user.name \"Test User\"");
        RunGit("config commit.gpgsign false");
        RunGit("config core.autocrlf false"); 
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectory))
        {
            ForceDeleteDirectory(_tempDirectory);
        }
    }

    private static void ForceDeleteDirectory(string path)
    {
        try 
        {
             foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
             {
                 File.SetAttributes(file, FileAttributes.Normal);
             }
             Directory.Delete(path, true);
        } 
        catch 
        { 
            // Ignore if clean-up fails
        }
    }

    private void RunGit(string args)
    {
        ProcessStartInfo psi = new()
        {
            FileName = "git",
            Arguments = args,
            WorkingDirectory = _repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process? process = Process.Start(psi);
        Assert.That(process, Is.Not.Null);
        process.WaitForExit();
        Assert.That(process.ExitCode, Is.Zero);
    }

    private string GetCurrentCommit()
    {
        ProcessStartInfo psi = new()
        {
            FileName = "git",
            Arguments = "rev-parse HEAD",
            WorkingDirectory = _repoRoot,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using Process? process = Process.Start(psi);
        Assert.That(process, Is.Not.Null);
        process.WaitForExit();
        return process.StandardOutput.ReadToEnd().Trim();
    }

    [Test]
    public void AnalyseDeltaCoverageDetectsChangedLines()
    {
        string filePath = Path.Combine(_repoRoot, "Class1.cs");
        // Using \n for consistency with core.autocrlf=false
        File.WriteAllText(filePath, "public class Class1\n{\n    public void Method1()\n    {\n        Console.WriteLine(\"Old\");\n    }\n}\n");
        RunGit("add .");
        RunGit("commit -m \"Initial commit\"");
        string baseCommit = GetCurrentCommit();

        // Original:
        // 1: public class Class1
        // 2: {
        // 3:     public void Method1()
        // 4:     {
        // 5:         Console.WriteLine(\"Old\");
        // 6:     }
        // 7: }

        // New:
        // 1: public class Class1
        // 2: {
        // 3:     public void Method1()
        // 4:     {
        // 5:         Console.WriteLine(\"Old\");
        // 6:         Console.WriteLine(\"New\"); // Inserted
        // 7:     }
        // 8: }
        File.WriteAllText(filePath, "public class Class1\n{\n    public void Method1()\n    {\n        Console.WriteLine(\"Old\");\n        Console.WriteLine(\"New\");\n    }\n}\n");
        RunGit("add .");
        RunGit("commit -m \"Update\"");

        // Coverage file covers the new line (Line 6)
        string coverageXml = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <coverage line-rate="1" branch-rate="1" version="1.9" timestamp="1600000000" lines-covered="6" lines-valid="6" branches-covered="0" branches-valid="0">
              <sources>
                <source>{_repoRoot}</source>
              </sources>
              <packages>
                <package name="TestPackage" line-rate="1" branch-rate="1" complexity="1">
                  <classes>
                    <class name="Class1" filename="Class1.cs" line-rate="1" branch-rate="1" complexity="1">
                      <methods>
                        <method name="Method1" signature="()V" line-rate="1" branch-rate="1" complexity="1">
                          <lines>
                            <line number="5" hits="1" branch="false" />
                            <line number="6" hits="1" branch="false" />
                          </lines>
                        </method>
                      </methods>
                      <lines>
                        <line number="5" hits="1" branch="false" />
                        <line number="6" hits="1" branch="false" />
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        string coverageFile = Path.Combine(_repoRoot, "coverage.cobertura.xml");
        File.WriteAllText(coverageFile, coverageXml);

        CoverageAnalyserOptions options = new()
        {
            CoverageFormat = CoverageFormat.Cobertura,
            Directory = _repoRoot,
            GlobPatterns = ["coverage.cobertura.xml"]
        };
        CoverageAnalyser analyser = new(options, NullLoggerFactory.Instance);
        DeltaResult result = analyser.AnalyseDeltaCoverage(baseCommit);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasChangedLines, Is.True);
            Assert.That(result.Coverage.Files, Has.Count.EqualTo(1));
        });

        FileCoverage file = result.Coverage.Files[0];
        Assert.That(file.Lines, Has.Count.EqualTo(1), "Should only detect one changed line");
        Assert.That(file.Lines[0].LineNumber, Is.EqualTo(6), "Line 6 should be the changed line");
    }

    [Test]
    public void AnalyseDeltaCoverageWithAutoFormatDetectsChangedLines()
    {
        string filePath = Path.Combine(_repoRoot, "Class1.cs");
        File.WriteAllText(filePath, "public class Class1\n{\n    public void Method1()\n    {\n        Console.WriteLine(\"Old\");\n    }\n}\n");
        RunGit("add .");
        RunGit("commit -m \"Initial commit\"");
        string baseCommit = GetCurrentCommit();

        File.WriteAllText(filePath, "public class Class1\n{\n    public void Method1()\n    {\n        Console.WriteLine(\"Old\");\n        Console.WriteLine(\"New\");\n    }\n}\n");
        RunGit("add .");
        RunGit("commit -m \"Update\"");

        string coverageXml = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <coverage line-rate="1" branch-rate="1" version="1.9" timestamp="1600000000" lines-covered="6" lines-valid="6" branches-covered="0" branches-valid="0">
              <sources>
                <source>{_repoRoot}</source>
              </sources>
              <packages>
                <package name="TestPackage" line-rate="1" branch-rate="1" complexity="1">
                  <classes>
                    <class name="Class1" filename="Class1.cs" line-rate="1" branch-rate="1" complexity="1">
                      <methods>
                        <method name="Method1" signature="()V" line-rate="1" branch-rate="1" complexity="1">
                          <lines>
                            <line number="5" hits="1" branch="false" />
                            <line number="6" hits="1" branch="false" />
                          </lines>
                        </method>
                      </methods>
                      <lines>
                        <line number="5" hits="1" branch="false" />
                        <line number="6" hits="1" branch="false" />
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        string coverageFile = Path.Combine(_repoRoot, "coverage.cobertura.xml");
        File.WriteAllText(coverageFile, coverageXml);

        // Using CoverageFormat.Auto here
        CoverageAnalyserOptions options = new()
        {
            CoverageFormat = CoverageFormat.Auto,
            Directory = _repoRoot,
            GlobPatterns = ["coverage.cobertura.xml"]
        };
        CoverageAnalyser analyser = new(options, NullLoggerFactory.Instance);
        DeltaResult result = analyser.AnalyseDeltaCoverage(baseCommit);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasChangedLines, Is.True);
            Assert.That(result.Coverage.Files, Has.Count.EqualTo(1));
        });

        FileCoverage file = result.Coverage.Files[0];
        Assert.That(file.Lines, Has.Count.EqualTo(1));
        Assert.That(file.Lines[0].LineNumber, Is.EqualTo(6));
    }
}
