using CoverageChecker.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoverageChecker.Tests.EndToEnd;

public class CoverageAnalyserOpenCoverTests
{
    private readonly string _directory = Path.Combine(TestContext.CurrentContext.TestDirectory, "CoverageFiles", "OpenCoverFormat");

    private CoverageAnalyser CreateAnalyser(string globPattern, CoverageFormat format = CoverageFormat.OpenCover, ILoggerFactory? loggerFactory = null)
    {
        CoverageAnalyserOptions options = new()
        {
            CoverageFormat = format,
            Directory = _directory,
            GlobPatterns = [globPattern]
        };
        return new CoverageAnalyser(options, loggerFactory);
    }

    [Test]
    public void CoverageAnalyserAnalyseOpenCoverCoverageWithLoggerReturnsCoverage()
    {
        Coverage coverage = CreateAnalyser("FullLineCoverage.xml", loggerFactory: NullLoggerFactory.Instance).AnalyseCoverage();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(2));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(3));
            Assert.That(coverage.Files[1].Lines, Has.Count.EqualTo(2));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(1));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        }
    }

    [Test]
    public void CoverageAnalyserAnalyseOpenCoverCoverageFullLineCoverageReturnsCoverage()
    {
        Coverage coverage = CreateAnalyser("FullLineCoverage.xml").AnalyseCoverage();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(2));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(1));
            Assert.That(coverage.Files[0].Lines[0].ClassName, Is.EqualTo("CoverageChecker.Tests.Sample"));
            Assert.That(coverage.Files[0].Lines[0].MethodName, Is.EqualTo("System.Void CoverageChecker.Tests.Sample::Covered()"));
            Assert.That(coverage.Files[0].Lines[0].MethodSignature, Is.EqualTo("System.Void CoverageChecker.Tests.Sample::Covered()"));
        }
    }

    [Test]
    public void CoverageAnalyserAnalyseOpenCoverCoveragePartialLineCoverageReturnsCoverage()
    {
        Coverage coverage = CreateAnalyser("PartialLineCoverage.xml").AnalyseCoverage();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(1));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(5));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo((double)2 / 5));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        }
    }

    [Test]
    public void CoverageAnalyserAnalyseOpenCoverCoverageFullBranchCoverageReturnsCoverage()
    {
        Coverage coverage = CreateAnalyser("FullBranchCoverage.xml").AnalyseCoverage();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(1));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(3));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(1));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.EqualTo((double)3 / 4));
            Assert.That(coverage.Files[0].Lines.Single(line => line.LineNumber == 20).Branches, Is.EqualTo(2));
            Assert.That(coverage.Files[0].Lines.Single(line => line.LineNumber == 20).CoveredBranches, Is.EqualTo(1));
            Assert.That(coverage.Files[0].Lines.Single(line => line.LineNumber == 21).Branches, Is.EqualTo(2));
            Assert.That(coverage.Files[0].Lines.Single(line => line.LineNumber == 21).CoveredBranches, Is.EqualTo(2));
        }
    }

    [Test]
    public void CoverageAnalyserAnalyseOpenCoverCoverageWithMethodFileRefAndOffsetBranchPointsReturnsCoverage()
    {
        Coverage coverage = CreateAnalyser("MethodFileRefBranchCoverage.xml").AnalyseCoverage();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(1));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(2));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(1));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.EqualTo((double)1 / 2));
            Assert.That(coverage.Files[0].Lines.Single(line => line.LineNumber == 31).Branches, Is.EqualTo(2));
            Assert.That(coverage.Files[0].Lines.Single(line => line.LineNumber == 31).CoveredBranches, Is.EqualTo(1));
        }
    }

    [TestCase("NoModules.xml")]
    [TestCase("NoFiles.xml")]
    [TestCase("NoSequencePoints.xml")]
    public void CoverageAnalyserAnalyseOpenCoverCoverageWithNoCoverageReturnsCoverage(string fixture)
    {
        Coverage coverage = CreateAnalyser(fixture).AnalyseCoverage();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(coverage.Files, Is.Empty);
            Assert.That(coverage.CalculateOverallCoverage(), Is.NaN);
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        }
    }

    [Test]
    public void CoverageAnalyserAnalyseOpenCoverCoverageAutoDetectsOpenCover()
    {
        Coverage coverage = CreateAnalyser("FullLineCoverage.xml", CoverageFormat.Auto).AnalyseCoverage();

        Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(1));
    }

    [Test]
    public void CoverageAnalyserAnalyseOpenCoverCoverageInvalidFileReferenceThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = CreateAnalyser("InvalidFileReference.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Is.EqualTo("OpenCover file id '99' was not found"));
    }
}
