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

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageWithLoggerReturnsCoverage()
    {
        Coverage coverage = CreateAnalyser("FullLineCoverage.xml", NullLoggerFactory.Instance).AnalyseCoverage();

        Assert.Multiple(() =>
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(3));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(6));
            Assert.That(coverage.Files[1].Lines, Has.Count.EqualTo(3));
            Assert.That(coverage.Files[2].Lines, Has.Count.EqualTo(2));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(1));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.EqualTo((double)5 / 6));
        });
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageFullLineCoverageReturnsCoverage()
    {
        Coverage coverage = CreateAnalyser("FullLineCoverage.xml").AnalyseCoverage();

        Assert.Multiple(() =>
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(3));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(6));
            Assert.That(coverage.Files[1].Lines, Has.Count.EqualTo(3));
            Assert.That(coverage.Files[2].Lines, Has.Count.EqualTo(2));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(1));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.EqualTo((double)5 / 6));
        });
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageFullBranchCoverageReturnsCoverage()
    {
        Coverage coverage = CreateAnalyser("FullBranchCoverage.xml").AnalyseCoverage();

        Assert.Multiple(() =>
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(2));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(5));
            Assert.That(coverage.Files[1].Lines, Has.Count.EqualTo(2));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(1));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.EqualTo(1));
        });
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoveragePartialLineCoverageReturnsCoverage()
    {
        Coverage coverage = CreateAnalyser("PartialLineCoverage.xml").AnalyseCoverage();

        Assert.Multiple(() =>
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(1));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(5));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo((double)1 / 5));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        });
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageNoPackagesReturnsCoverage()
    {
        Coverage coverage = CreateAnalyser("NoPackages.xml").AnalyseCoverage();

        Assert.Multiple(() =>
        {
            Assert.That(coverage.Files, Is.Empty);
            Assert.That(coverage.CalculateOverallCoverage(), Is.NaN);
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        });
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageNoClassesReturnsCoverage()
    {
        Coverage coverage = CreateAnalyser("NoClasses.xml").AnalyseCoverage();

        Assert.Multiple(() =>
        {
            Assert.That(coverage.Files, Is.Empty);
            Assert.That(coverage.CalculateOverallCoverage(), Is.NaN);
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        });
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageNoLinesReturnsCoverage()
    {
        Coverage coverage = CreateAnalyser("NoLines.xml").AnalyseCoverage();

        Assert.Multiple(() =>
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(2));
            Assert.That(coverage.Files[0].Lines, Is.Empty);
            Assert.That(coverage.Files[1].Lines, Is.Empty);
            Assert.That(coverage.CalculateOverallCoverage(), Is.NaN);
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        });
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageWithSourcesReturnsCoverage()
    {
        Coverage coverage = CreateAnalyser(["Sources1.xml", "Sources2.xml"]).AnalyseCoverage();

        Assert.Multiple(() =>
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(1));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(3));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(1));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        });
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
    public void CoverageAnalyserAnalyseCoberturaCoverageMultipleSourcesThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = CreateAnalyser("MultipleSources.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Is.EqualTo("Multiple sources are not supported"));
    }
}
