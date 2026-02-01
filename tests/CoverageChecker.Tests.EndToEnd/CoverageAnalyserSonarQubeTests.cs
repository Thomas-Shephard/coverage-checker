using CoverageChecker.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoverageChecker.Tests.EndToEnd;

public class CoverageAnalyserSonarQubeTests
{
    private readonly string _directory = Path.Combine(TestContext.CurrentContext.TestDirectory, "CoverageFiles", "SonarQube");

    [Test]
    public void CoverageAnalyserAnalyseSonarQubeCoverageWithLoggerReturnsCoverage()
    {
        Coverage coverage = new CoverageAnalyser(CoverageFormat.SonarQube, _directory, "FullLineCoverage.xml", NullLoggerFactory.Instance).AnalyseCoverage();

        Assert.Multiple(() =>
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(4));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(2));
            Assert.That(coverage.Files[1].Lines, Has.Count.EqualTo(4));
            Assert.That(coverage.Files[2].Lines, Has.Count.EqualTo(3));
            Assert.That(coverage.Files[3].Lines, Has.Count.EqualTo(2));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(1));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.EqualTo((double)5 / 6));
        });
    }

    [Test]
    public void CoverageAnalyserAnalyseSonarQubeCoverageFullLineCoverageReturnsCoverage()
    {
        Coverage coverage = new CoverageAnalyser(CoverageFormat.SonarQube, _directory, "FullLineCoverage.xml").AnalyseCoverage();

        Assert.Multiple(() =>
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(4));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(2));
            Assert.That(coverage.Files[1].Lines, Has.Count.EqualTo(4));
            Assert.That(coverage.Files[2].Lines, Has.Count.EqualTo(3));
            Assert.That(coverage.Files[3].Lines, Has.Count.EqualTo(2));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(1));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.EqualTo((double)5 / 6));
        });
    }

    [Test]
    public void CoverageAnalyserAnalyseSonarQubeCoverageFullBranchCoverageReturnsCoverage()
    {
        Coverage coverage = new CoverageAnalyser(CoverageFormat.SonarQube, _directory, "FullBranchCoverage.xml").AnalyseCoverage();

        Assert.Multiple(() =>
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(3));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(2));
            Assert.That(coverage.Files[1].Lines, Has.Count.EqualTo(3));
            Assert.That(coverage.Files[2].Lines, Has.Count.EqualTo(2));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(1));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.EqualTo(1));
        });
    }

    [Test]
    public void CoverageAnalyserAnalyseSonarQubeCoveragePartialLineCoverageReturnsCoverage()
    {
        Coverage coverage = new CoverageAnalyser(CoverageFormat.SonarQube, _directory, "PartialLineCoverage.xml").AnalyseCoverage();

        Assert.Multiple(() =>
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(2));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(2));
            Assert.That(coverage.Files[1].Lines, Has.Count.EqualTo(3));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo((double)1 / 5));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        });
    }

    [Test]
    public void CoverageAnalyserAnalyseSonarQubeCoverageNoFilesReturnsCoverage()
    {
        Coverage coverage = new CoverageAnalyser(CoverageFormat.SonarQube, _directory, "NoFiles.xml").AnalyseCoverage();

        Assert.Multiple(() =>
        {
            Assert.That(coverage.Files, Is.Empty);
            Assert.That(coverage.CalculateOverallCoverage(), Is.NaN);
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        });
    }

    [Test]
    public void CoverageAnalyserAnalyseSonarQubeCoverageNoLinesReturnsCoverage()
    {
        Coverage coverage = new CoverageAnalyser(CoverageFormat.SonarQube, _directory, "NoLines.xml").AnalyseCoverage();

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
    public void CoverageAnalyserAnalyseSonarQubeCoverageInvalidVersionThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = new(CoverageFormat.SonarQube, _directory, "InvalidVersion.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Is.EqualTo("Attribute 'version' on element 'coverage' must be '1'"));
    }

    [Test]
    public void CoverageAnalyserAnalyseSonarQubeCoverageEmptyFileThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = new(CoverageFormat.SonarQube, _directory, "EmptyFile.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Does.StartWith("Failed to load coverage file"));
    }

    [Test]
    public void CoverageAnalyserAnalyseSonarQubeCoverageInvalidFileSetup1ThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = new(CoverageFormat.SonarQube, _directory, "InvalidFileSetup1.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Is.EqualTo("Expected coverage to be the root element"));
    }

    [Test]
    public void CoverageAnalyserAnalyseSonarQubeCoverageInvalidFileSetup2ThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = new(CoverageFormat.SonarQube, _directory, "InvalidFileSetup2.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Is.EqualTo("Expected coverage to be the root element"));
    }

    [Test]
    public void CoverageAnalyserAnalyseSonarQubeCoverageInvalidFileThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = new(CoverageFormat.SonarQube, _directory, "InvalidFile.xml");

        Assert.Throws<NoCoverageFilesFoundException>(() => coverageAnalyser.AnalyseCoverage());
    }

    [Test]
    public void CoverageAnalyserAnalyseSonarQubeCoverageInconsistentBranchesThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = new(CoverageFormat.SonarQube, _directory, "InconsistentBranches.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Is.EqualTo("Both 'branchesToCover' and 'coveredBranches' attributes must be present if either is specified"));
    }
}