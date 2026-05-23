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

    private CoverageAnalyser CreateAnalyser(IEnumerable<string> globPatterns)
    {
        CoverageAnalyserOptions options = new()
        {
            CoverageFormat = CoverageFormat.OpenCover,
            Directory = _directory,
            GlobPatterns = globPatterns
        };
        return new CoverageAnalyser(options);
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

    [Test]
    public void CoverageAnalyserAnalyseOpenCoverCoverageWithSequencePointBranchTotalsReturnsCoverage()
    {
        Coverage coverage = CreateAnalyser("SequencePointBranchTotals.xml").AnalyseCoverage();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(1));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(1));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.EqualTo((double)1 / 2));
            Assert.That(coverage.Files[0].Lines[0].Branches, Is.EqualTo(2));
            Assert.That(coverage.Files[0].Lines[0].CoveredBranches, Is.EqualTo(1));
        }
    }

    [Test]
    public void CoverageAnalyserAnalyseOpenCoverCoverageIgnoresSequencePointBranchTotalsAfterBranchPoints()
    {
        Coverage coverage = CreateAnalyser("BranchTotalsIgnoredAfterBranchPoints.xml").AnalyseCoverage();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(1));
            Assert.That(coverage.Files[0].Lines[0].Branches, Is.EqualTo(2));
            Assert.That(coverage.Files[0].Lines[0].CoveredBranches, Is.EqualTo(1));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.EqualTo(0.5));
        }
    }

    [Test]
    public void CoverageAnalyserAnalyseOpenCoverCoverageSkipsUnmatchedOffsetBranchPoint()
    {
        Coverage coverage = CreateAnalyser("UnmatchedOffsetBranchPoint.xml").AnalyseCoverage();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(1));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(1));
            Assert.That(coverage.Files[0].Lines[0].Branches, Is.Null);
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        }
    }

    [Test]
    public void CoverageAnalyserAnalyseOpenCoverCoverageUsesMethodFileRefForBranchPointLineNumber()
    {
        Coverage coverage = CreateAnalyser("BranchPointLineNumberWithFileRef.xml").AnalyseCoverage();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(1));
            Assert.That(coverage.Files[0].Lines.Single(line => line.LineNumber == 55).Branches, Is.EqualTo(1));
            Assert.That(coverage.Files[0].Lines.Single(line => line.LineNumber == 55).CoveredBranches, Is.EqualTo(1));
        }
    }

    [Test]
    public void CoverageAnalyserAnalyseOpenCoverCoverageUsesParentSequencePointFileForBranchPointLineNumber()
    {
        Coverage coverage = CreateAnalyser("BranchPointLineNumberWithParentSequencePoint.xml").AnalyseCoverage();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(1));
            Assert.That(coverage.Files[0].Lines.Single(line => line.LineNumber == 56).Branches, Is.EqualTo(1));
            Assert.That(coverage.Files[0].Lines.Single(line => line.LineNumber == 56).CoveredBranches, Is.EqualTo(1));
        }
    }

    [Test]
    public void CoverageAnalyserAnalyseOpenCoverCoverageUsesBranchPointFileAndParentSequencePointLineNumber()
    {
        Coverage coverage = CreateAnalyser("BranchPointFileWithParentSequencePointLineNumber.xml").AnalyseCoverage();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(1));
            Assert.That(coverage.Files[0].Lines.Single(line => line.LineNumber == 57).Branches, Is.EqualTo(1));
            Assert.That(coverage.Files[0].Lines.Single(line => line.LineNumber == 57).CoveredBranches, Is.EqualTo(1));
        }
    }

    [Test]
    public void CoverageAnalyserAnalyseOpenCoverCoverageDuplicateSequencePointKeepsMetadata()
    {
        Coverage coverage = CreateAnalyser("DuplicateSequencePointSameLine.xml").AnalyseCoverage();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(1));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(1));
            Assert.That(coverage.Files[0].Lines[0].ClassName, Is.EqualTo("CoverageChecker.Tests.DuplicateSequencePoint"));
            Assert.That(coverage.Files[0].Lines[0].MethodName, Is.EqualTo("System.Void CoverageChecker.Tests.DuplicateSequencePoint::SameLine()"));
        }
    }

    [Test]
    public void CoverageAnalyserAnalyseOpenCoverCoverageMergesKnownBranchCountsAcrossReports()
    {
        Coverage coverage = CreateAnalyser(["KnownBranchesWithBranch.xml", "KnownBranchesWithoutBranch.xml"]).AnalyseCoverage();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(1));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(1));
            Assert.That(coverage.Files[0].Lines[0].Branches, Is.EqualTo(2));
            Assert.That(coverage.Files[0].Lines[0].CoveredBranches, Is.EqualTo(1));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.EqualTo(0.5));
        }
    }

    [Test]
    public void CoverageAnalyserAnalyseOpenCoverCoverageConflictingMetadataClearsMetadata()
    {
        Coverage coverage = CreateAnalyser(["ConflictingMetadata1.xml", "ConflictingMetadata2.xml"]).AnalyseCoverage();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(coverage.Files, Has.Count.EqualTo(1));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(1));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(1));
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

    [Test]
    public void CoverageAnalyserAnalyseOpenCoverCoverageInvalidBranchPointFileReferenceThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = CreateAnalyser("InvalidBranchPointFileReference.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Is.EqualTo("OpenCover file id '99' was not found"));
    }

    [Test]
    public void CoverageAnalyserAnalyseOpenCoverCoverageInvalidRootThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = CreateAnalyser("InvalidRoot.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Is.EqualTo("Expected CoverageSession to be the root element"));
    }

    [Test]
    public void CoverageAnalyserAnalyseOpenCoverCoverageInvalidFileRefThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = CreateAnalyser("InvalidFileRef.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Is.EqualTo("OpenCover file id '99' was not found"));
    }

    [Test]
    public void CoverageAnalyserAnalyseOpenCoverCoverageMissingSequencePointFileReferenceThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = CreateAnalyser("MissingSequencePointFileReference.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Is.EqualTo("Attribute 'fileid' not found on element 'SequencePoint' and no method FileRef was available"));
    }

    [Test]
    public void CoverageAnalyserAnalyseOpenCoverCoverageMissingBranchPointFileReferenceThrowsCoverageParseException()
    {
        CoverageAnalyser coverageAnalyser = CreateAnalyser("MissingBranchPointFileReference.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Is.EqualTo("Attribute 'fileid' not found on element 'BranchPoint' and no method FileRef was available"));
    }
}
