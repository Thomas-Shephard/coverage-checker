using CoverageChecker.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoverageChecker.Tests.EndToEnd;

public class CoverageAnalyserCoberturaTests
{
    private readonly string _directory = Path.Combine(TestContext.CurrentContext.TestDirectory, "CoverageFiles", "Cobertura");

    private CoverageAnalyser CreateAnalyser(string globPattern, ILoggerFactory? loggerFactory = null)
    {
        CoverageAnalyserOptions options = new()
        {
            CoverageFormat = CoverageFormat.Cobertura,
            Directory = _directory,
            GlobPatterns = [globPattern]
        };
        return new CoverageAnalyser(options, loggerFactory);
    }

    private CoverageAnalyser CreateAnalyser(IEnumerable<string> globPatterns)
    {
        CoverageAnalyserOptions options = new()
        {
            CoverageFormat = CoverageFormat.Cobertura,
            Directory = _directory,
            GlobPatterns = globPatterns
        };
        return new CoverageAnalyser(options);
    }

    private static string GetRepoRoot()
    {
        DirectoryInfo? directory = new(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CoverageChecker.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not find repository root");
    }

    private static string NormalizeFullPath(params string[] paths)
    {
        string path = Path.GetFullPath(Path.Combine(paths)).Replace(Path.DirectorySeparatorChar, '/');
        string trimmed = path.TrimEnd('/');

        return trimmed switch
        {
            "" when path.Length > 0 => "/",
            _ when trimmed.Length == 2 && char.IsLetter(trimmed[0]) && trimmed[1] == ':' => trimmed + "/",
            _ => trimmed
        };
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageWithLoggerReturnsCoverage()
    {
        Coverage coverage = CreateAnalyser("FullLineCoverage.xml", NullLoggerFactory.Instance).AnalyseCoverage();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(3));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(6));
            Assert.That(coverage.Files[1].Lines, Has.Count.EqualTo(3));
            Assert.That(coverage.Files[2].Lines, Has.Count.EqualTo(2));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(1));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.EqualTo((double)5 / 6));
        }
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageFullLineCoverageReturnsCoverage()
    {
        Coverage coverage = CreateAnalyser("FullLineCoverage.xml").AnalyseCoverage();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(3));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(6));
            Assert.That(coverage.Files[1].Lines, Has.Count.EqualTo(3));
            Assert.That(coverage.Files[2].Lines, Has.Count.EqualTo(2));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(1));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.EqualTo((double)5 / 6));
        }
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageFullBranchCoverageReturnsCoverage()
    {
        Coverage coverage = CreateAnalyser("FullBranchCoverage.xml").AnalyseCoverage();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(2));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(5));
            Assert.That(coverage.Files[1].Lines, Has.Count.EqualTo(2));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(1));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.EqualTo(1));
        }
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoveragePartialLineCoverageReturnsCoverage()
    {
        Coverage coverage = CreateAnalyser("PartialLineCoverage.xml").AnalyseCoverage();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(1));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(5));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo((double)1 / 5));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        }
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageNoPackagesReturnsCoverage()
    {
        Coverage coverage = CreateAnalyser("NoPackages.xml").AnalyseCoverage();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(coverage.Files, Is.Empty);
            Assert.That(coverage.CalculateOverallCoverage(), Is.NaN);
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        }
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageNoClassesReturnsCoverage()
    {
        Coverage coverage = CreateAnalyser("NoClasses.xml").AnalyseCoverage();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(coverage.Files, Is.Empty);
            Assert.That(coverage.CalculateOverallCoverage(), Is.NaN);
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        }
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageNoLinesReturnsCoverage()
    {
        Coverage coverage = CreateAnalyser("NoLines.xml").AnalyseCoverage();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(2));
            Assert.That(coverage.Files[0].Lines, Is.Empty);
            Assert.That(coverage.Files[1].Lines, Is.Empty);
            Assert.That(coverage.CalculateOverallCoverage(), Is.NaN);
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        }
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageWithSourcesReturnsCoverage()
    {
        Coverage coverage = CreateAnalyser(["Sources1.xml", "Sources2.xml"]).AnalyseCoverage();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(1));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(3));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(1));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        }
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageInvalidBranchCoverage1ThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = CreateAnalyser("InvalidBranchCoverage1.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Is.EqualTo("Attribute 'condition-coverage' on element 'line' is not in the correct format"));
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageInvalidBranchCoverage2ThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = CreateAnalyser("InvalidBranchCoverage2.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Is.EqualTo("Attribute 'condition-coverage' on element 'line' is not in the correct format"));
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageInvalidBranchCoverage3ThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = CreateAnalyser("InvalidBranchCoverage3.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Is.EqualTo("Attribute 'condition-coverage' on element 'line' is not in the correct format"));
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageEmptyFileThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = CreateAnalyser("EmptyFile.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Does.StartWith("Failed to load coverage file"));
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageInvalidFileSetup1ThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = CreateAnalyser("InvalidFileSetup1.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Is.EqualTo("Expected coverage to be the root element"));
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageInvalidFileSetup2ThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = CreateAnalyser("InvalidFileSetup2.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Is.EqualTo("Expected coverage to be the root element"));
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageInvalidFileThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = CreateAnalyser("InvalidFile.xml");

        Assert.Throws<NoCoverageFilesFoundException>(() => coverageAnalyser.AnalyseCoverage());
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageMultipleSourcesWithExistingFileUsesMatchingSource()
    {
        Coverage coverage = CreateAnalyser("MultipleSourcesSecondSource.xml").AnalyseCoverage();

        string expectedPath = NormalizeFullPath(GetRepoRoot(), "tests", "CoverageChecker.Tests.EndToEnd", "CoverageFiles", "Cobertura", "MultipleSources", "Second", "file-1.txt");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(1));
            Assert.That(coverage.Files[0].Path, Is.EqualTo(expectedPath));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(4));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(0.5));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        }
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageMultipleSourcesWithNoMatchingFileUsesFirstSourceFallback()
    {
        Coverage coverage = CreateAnalyser("MultipleSources.xml").AnalyseCoverage();

        string expectedPath = NormalizeFullPath(GetRepoRoot(), "FilePathPart1", "file-1");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(1));
            Assert.That(coverage.Files[0].Path, Is.EqualTo(expectedPath));
            Assert.That(coverage.Files[0].Lines, Is.Empty);
            Assert.That(coverage.CalculateOverallCoverage(), Is.NaN);
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        }
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageAbsoluteFileNameIgnoresSources()
    {
        const string coverageFileName = "MultipleSourcesAbsoluteFilename.xml";
        string coverageFilePath = Path.Combine(_directory, coverageFileName);
        string expectedPath = NormalizeFullPath(GetRepoRoot(), "tests", "CoverageChecker.Tests.EndToEnd", "CoverageFiles", "Cobertura", "MultipleSources", "Second", "file-1.txt");

        File.WriteAllText(coverageFilePath, $$"""
            <?xml version="1.0" encoding="utf-8"?>
            <coverage>
                <sources>
                    <source>FilePathPart1/</source>
                    <source>FilePathPart2/</source>
                </sources>
                <packages>
                    <package name="package-1">
                        <classes>
                            <class name="class-1" filename="{{expectedPath}}">
                                <methods/>
                                <lines>
                                    <line number="1" hits="1"/>
                                </lines>
                            </class>
                        </classes>
                    </package>
                </packages>
            </coverage>
            """);

        try
        {
            Coverage coverage = CreateAnalyser(coverageFileName).AnalyseCoverage();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(coverage.Files, Has.Count.EqualTo(1));
                Assert.That(coverage.Files[0].Path, Is.EqualTo(expectedPath));
                Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(1));
                Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(1));
                Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
            }
        }
        finally
        {
            File.Delete(coverageFilePath);
        }
    }
}
