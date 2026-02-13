using CoverageChecker.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoverageChecker.Tests.EndToEnd;

public class CoverageAnalyserCoberturaTests
{
    private readonly string _directory = Path.Combine(TestContext.CurrentContext.TestDirectory, "CoverageFiles", "Cobertura");

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageWithLoggerReturnsCoverage()
    {
        Coverage coverage = new CoverageAnalyser(CoverageFormat.Cobertura, _directory, "FullLineCoverage.xml", NullLoggerFactory.Instance).AnalyseCoverage();

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
        Coverage coverage = new CoverageAnalyser(CoverageFormat.Cobertura, _directory, "FullLineCoverage.xml").AnalyseCoverage();

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
        Coverage coverage = new CoverageAnalyser(CoverageFormat.Cobertura, _directory, "FullBranchCoverage.xml").AnalyseCoverage();

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
        Coverage coverage = new CoverageAnalyser(CoverageFormat.Cobertura, _directory, "PartialLineCoverage.xml").AnalyseCoverage();

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
        Coverage coverage = new CoverageAnalyser(CoverageFormat.Cobertura, _directory, "NoPackages.xml").AnalyseCoverage();

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
        Coverage coverage = new CoverageAnalyser(CoverageFormat.Cobertura, _directory, "NoClasses.xml").AnalyseCoverage();

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
        Coverage coverage = new CoverageAnalyser(CoverageFormat.Cobertura, _directory, "NoLines.xml").AnalyseCoverage();

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
        Coverage coverage = new CoverageAnalyser(CoverageFormat.Cobertura, _directory, ["Sources1.xml", "Sources2.xml"]).AnalyseCoverage();

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
        CoverageAnalyser coverageAnalyser = new(CoverageFormat.Cobertura, _directory, "InvalidBranchCoverage1.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Is.EqualTo("Attribute 'condition-coverage' on element 'line' is not in the correct format"));
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageInvalidBranchCoverage2ThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = new(CoverageFormat.Cobertura, _directory, "InvalidBranchCoverage2.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Is.EqualTo("Attribute 'condition-coverage' on element 'line' is not in the correct format"));
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageInvalidBranchCoverage3ThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = new(CoverageFormat.Cobertura, _directory, "InvalidBranchCoverage3.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Is.EqualTo("Attribute 'condition-coverage' on element 'line' is not in the correct format"));
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageEmptyFileThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = new(CoverageFormat.Cobertura, _directory, "EmptyFile.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Does.StartWith("Failed to load coverage file"));
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageInvalidFileSetup1ThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = new(CoverageFormat.Cobertura, _directory, "InvalidFileSetup1.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Is.EqualTo("Expected coverage to be the root element"));
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageInvalidFileSetup2ThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = new(CoverageFormat.Cobertura, _directory, "InvalidFileSetup2.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Is.EqualTo("Expected coverage to be the root element"));
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageInvalidFileThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = new(CoverageFormat.Cobertura, _directory, "InvalidFile.xml");

        Assert.Throws<NoCoverageFilesFoundException>(() => coverageAnalyser.AnalyseCoverage());
    }

    [Test]
    public void CoverageAnalyserAnalyseCoberturaCoverageMultipleSourcesThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = new(CoverageFormat.Cobertura, _directory, "MultipleSources.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Is.EqualTo("Multiple sources are not supported"));
    }
}