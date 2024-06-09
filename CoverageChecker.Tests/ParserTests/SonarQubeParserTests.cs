using CoverageChecker.Parsers;
using CoverageChecker.Results;

namespace CoverageChecker.Tests.ParserTests;

public class SonarQubeParserTests {
    private readonly string _directory = Path.Combine(TestContext.CurrentContext.TestDirectory, "CoverageFiles", "SonarQube");

    [Test]
    public void SonarQubeParser_LoadCoverage_FullLineCoverage_ReturnsCoverage() {
        SonarQubeParser sonarQubeParser = new(_directory, "FullLineCoverage.xml");

        Coverage coverage = sonarQubeParser.LoadCoverage();

        Assert.Multiple(() => {
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
    public void SonarQubeParser_LoadCoverage_FullBranchCoverage_ReturnsCoverage() {
        SonarQubeParser sonarQubeParser = new(_directory, "FullBranchCoverage.xml");

        Coverage coverage = sonarQubeParser.LoadCoverage();

        Assert.Multiple(() => {
            Assert.That(coverage.Files, Has.Count.EqualTo(3));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(2));
            Assert.That(coverage.Files[1].Lines, Has.Count.EqualTo(3));
            Assert.That(coverage.Files[2].Lines, Has.Count.EqualTo(2));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(1));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.EqualTo(1));
        });
    }

    [Test]
    public void SonarQubeParser_LoadCoverage_PartialLineCoverage_ReturnsCoverage() {
        SonarQubeParser sonarQubeParser = new(_directory, "PartialLineCoverage.xml");

        Coverage coverage = sonarQubeParser.LoadCoverage();

        Assert.Multiple(() => {
            Assert.That(coverage.Files, Has.Count.EqualTo(2));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(2));
            Assert.That(coverage.Files[1].Lines, Has.Count.EqualTo(3));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo((double)1 / 5));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        });
    }

    [Test]
    public void SonarQubeParser_LoadCoverage_NoFiles_ReturnsCoverage() {
        SonarQubeParser sonarQubeParser = new(_directory, "NoFiles.xml");

        Coverage coverage = sonarQubeParser.LoadCoverage();

        Assert.Multiple(() => {
            Assert.That(coverage.Files, Is.Empty);
            Assert.That(coverage.CalculateOverallCoverage(), Is.NaN);
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        });
    }

    [Test]
    public void SonarQubeParser_LoadCoverage_NoLines_ReturnsCoverage() {
        SonarQubeParser sonarQubeParser = new(_directory, "NoLines.xml");

        Coverage coverage = sonarQubeParser.LoadCoverage();

        Assert.Multiple(() => {
            Assert.That(coverage.Files, Has.Count.EqualTo(2));
            Assert.That(coverage.Files[0].Lines, Is.Empty);
            Assert.That(coverage.Files[1].Lines, Is.Empty);
            Assert.That(coverage.CalculateOverallCoverage(), Is.NaN);
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        });
    }

    [Test]
    public void SonarQubeParser_LoadCoverage_InvalidVersion_ThrowsCoverageParseException() {
        SonarQubeParser sonarQubeParser = new(_directory, "InvalidVersion.xml");

        Assert.Throws<CoverageParseException>(() => sonarQubeParser.LoadCoverage());
    }

    [Test]
    public void SonarQubeParser_LoadCoverage_EmptyFile_ThrowsCoverageParseException() {
        SonarQubeParser sonarQubeParser = new(_directory, "EmptyFile.xml");

        Assert.Throws<CoverageParseException>(() => sonarQubeParser.LoadCoverage());
    }

    [Test]
    public void SonarQubeParser_LoadCoverage_InvalidFile_ThrowsCoverageParseException() {
        SonarQubeParser sonarQubeParser = new(_directory, "InvalidFile.xml");

        Assert.Throws<CoverageParseException>(() => sonarQubeParser.LoadCoverage());
    }
}