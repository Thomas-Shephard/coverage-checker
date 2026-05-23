using CoverageChecker.Parsers;
using CoverageChecker.Results;
using CoverageChecker.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoverageChecker.Tests.Unit.ParserTests;

public class OpenCoverParserTests
{
    [Test]
    public void ParseCoverageWithKnownBranchCountThenMissingBranchCountUsesKnownBranchCount()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString());
        Directory.CreateDirectory(directory);

        string coverageWithBranches = Path.Combine(directory, "with-branches.xml");
        string coverageWithoutBranches = Path.Combine(directory, "without-branches.xml");

        File.WriteAllText(coverageWithBranches, CreateCoverageFile("""
            <SequencePoint vc="1" uspid="1" ordinal="0" offset="0" sl="60" fileid="1" />
            """, """
            <BranchPoint vc="1" uspid="2" ordinal="0" offset="0" sl="60" fileid="1" path="0" />
            <BranchPoint vc="0" uspid="3" ordinal="1" offset="0" sl="60" fileid="1" path="1" />
            """));

        File.WriteAllText(coverageWithoutBranches, CreateCoverageFile("""
            <SequencePoint vc="0" uspid="1" ordinal="0" offset="0" sl="60" fileid="1" />
            """, ""));

        try
        {
            Coverage coverage = new();
            OpenCoverParser parser = new(coverage, NullLogger<OpenCoverParser>.Instance, new CoverageMergeService());

            parser.ParseCoverage(coverageWithBranches);
            parser.ParseCoverage(coverageWithoutBranches);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(coverage.Files, Has.Count.EqualTo(1));
                Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(1));
                Assert.That(coverage.Files[0].Lines[0].Branches, Is.EqualTo(2));
                Assert.That(coverage.Files[0].Lines[0].CoveredBranches, Is.EqualTo(1));
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ParseCoverageWithHigherKnownBranchCountThenLowerBranchCountUsesKnownBranchCount()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString());
        Directory.CreateDirectory(directory);

        string coverageWithFourBranches = Path.Combine(directory, "with-four-branches.xml");
        string coverageWithTwoBranches = Path.Combine(directory, "with-two-branches.xml");

        File.WriteAllText(coverageWithFourBranches, CreateCoverageFile("""
            <SequencePoint vc="1" uspid="1" ordinal="0" offset="0" sl="60" fileid="1" />
            """, """
            <BranchPoint vc="1" uspid="2" ordinal="0" offset="0" sl="60" fileid="1" path="0" />
            <BranchPoint vc="1" uspid="3" ordinal="1" offset="0" sl="60" fileid="1" path="1" />
            <BranchPoint vc="1" uspid="4" ordinal="2" offset="0" sl="60" fileid="1" path="2" />
            <BranchPoint vc="1" uspid="5" ordinal="3" offset="0" sl="60" fileid="1" path="3" />
            """));

        File.WriteAllText(coverageWithTwoBranches, CreateCoverageFile("""
            <SequencePoint vc="1" uspid="1" ordinal="0" offset="0" sl="60" fileid="1" />
            """, """
            <BranchPoint vc="1" uspid="2" ordinal="0" offset="0" sl="60" fileid="1" path="0" />
            <BranchPoint vc="1" uspid="3" ordinal="1" offset="0" sl="60" fileid="1" path="1" />
            """));

        try
        {
            Coverage coverage = new();
            OpenCoverParser parser = new(coverage, NullLogger<OpenCoverParser>.Instance, new CoverageMergeService());

            parser.ParseCoverage(coverageWithFourBranches);
            parser.ParseCoverage(coverageWithTwoBranches);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(coverage.Files, Has.Count.EqualTo(1));
                Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(1));
                Assert.That(coverage.Files[0].Lines[0].Branches, Is.EqualTo(4));
                Assert.That(coverage.Files[0].Lines[0].CoveredBranches, Is.EqualTo(4));
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static string CreateCoverageFile(string sequencePoints, string branchPoints)
    {
        return $$"""
            <?xml version="1.0" encoding="utf-8"?>
            <CoverageSession>
              <Modules>
                <Module>
                  <Files>
                    <File uid="1" fullPath="tests/CoverageChecker.Tests.Unit/ParserTests/known-branches.cs" />
                  </Files>
                  <Classes>
                    <Class>
                      <FullName>CoverageChecker.Tests.KnownBranches</FullName>
                      <Methods>
                        <Method>
                          <Name>System.Void CoverageChecker.Tests.KnownBranches::WithBranch()</Name>
                          <SequencePoints>
            {{sequencePoints}}
                          </SequencePoints>
                          <BranchPoints>
            {{branchPoints}}
                          </BranchPoints>
                        </Method>
                      </Methods>
                    </Class>
                  </Classes>
                </Module>
              </Modules>
            </CoverageSession>
            """;
    }
}
