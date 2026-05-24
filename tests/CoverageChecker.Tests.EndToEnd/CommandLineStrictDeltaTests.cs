namespace CoverageChecker.Tests.EndToEnd;

internal sealed class CommandLineStrictDeltaTests : CommandLineTestBase
{
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

}
