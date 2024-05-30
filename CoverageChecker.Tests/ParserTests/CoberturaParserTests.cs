using CoverageChecker.Parsers;
using CoverageChecker.Results;

namespace CoverageChecker.Tests.ParserTests;

public class CoberturaParserTests {
    private readonly string _directory = Path.Combine(TestContext.CurrentContext.TestDirectory, "CoverageFiles", "Cobertura");

    [Test]
    public void SonarQubeParser_LoadCoverage_FullLineCoverage_ReturnsCoverage() {
        CoberturaParser coberturaParser = new("FullLineCoverage.xml", _directory);

        Coverage coverage = coberturaParser.LoadCoverage();

        Assert.Multiple(() => {
            Assert.That(coverage.Files, Has.Length.EqualTo(4));
            Assert.That(coverage.Files[0].Lines, Has.Length.EqualTo(2));
            Assert.That(coverage.Files[1].Lines, Has.Length.EqualTo(4));
            Assert.That(coverage.Files[2].Lines, Has.Length.EqualTo(3));
            Assert.That(coverage.Files[3].Lines, Has.Length.EqualTo(2));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(1));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.EqualTo((double)5 / 6));
        });
    }

    [Test]
    public void SonarQubeParser_LoadCoverage_FullBranchCoverage_ReturnsCoverage() {
        CoberturaParser coberturaParser = new("FullBranchCoverage.xml", _directory);

        Coverage coverage = coberturaParser.LoadCoverage();

        Assert.Multiple(() => {
            Assert.That(coverage.Files, Has.Length.EqualTo(3));
            Assert.That(coverage.Files[0].Lines, Has.Length.EqualTo(2));
            Assert.That(coverage.Files[1].Lines, Has.Length.EqualTo(3));
            Assert.That(coverage.Files[2].Lines, Has.Length.EqualTo(2));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(1));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.EqualTo(1));
        });
    }

    [Test]
    public void SonarQubeParser_LoadCoverage_PartialLineCoverage_ReturnsCoverage() {
        CoberturaParser coberturaParser = new("PartialLineCoverage.xml", _directory);

        Coverage coverage = coberturaParser.LoadCoverage();

        Assert.Multiple(() => {
            Assert.That(coverage.Files, Has.Length.EqualTo(2));
            Assert.That(coverage.Files[0].Lines, Has.Length.EqualTo(2));
            Assert.That(coverage.Files[1].Lines, Has.Length.EqualTo(3));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo((double)1 / 5));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        });
    }

    [Test]
    public void SonarQubeParser_LoadCoverage_NoPackages_ReturnsCoverage() {
        CoberturaParser coberturaParser = new("NoPackages.xml", _directory);

        Coverage coverage = coberturaParser.LoadCoverage();

        Assert.Multiple(() => {
            Assert.That(coverage.Files, Is.Empty);
            Assert.That(coverage.CalculateOverallCoverage(), Is.NaN);
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        });
    }

    [Test]
    public void SonarQubeParser_LoadCoverage_NoClasses_ReturnsCoverage() {
        CoberturaParser coberturaParser = new("NoClasses.xml", _directory);

        Coverage coverage = coberturaParser.LoadCoverage();

        Assert.Multiple(() => {
            Assert.That(coverage.Files, Is.Empty);
            Assert.That(coverage.CalculateOverallCoverage(), Is.NaN);
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        });
    }

    [Test]
    public void SonarQubeParser_LoadCoverage_NoLines_ReturnsCoverage() {
        CoberturaParser coberturaParser = new("NoLines.xml", _directory);

        Coverage coverage = coberturaParser.LoadCoverage();

        Assert.Multiple(() => {
            Assert.That(coverage.Files, Has.Length.EqualTo(2));
            Assert.That(coverage.Files[0].Lines, Is.Empty);
            Assert.That(coverage.Files[1].Lines, Is.Empty);
            Assert.That(coverage.CalculateOverallCoverage(), Is.NaN);
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        });
    }

    [Test]
    public void SonarQubeParser_LoadCoverage_InvalidVersion_ThrowsCoverageParseException() {
        CoberturaParser coberturaParser = new("InvalidVersion.xml", _directory);

        Assert.Throws<CoverageParseException>(() => coberturaParser.LoadCoverage());
    }

    [Test]
    public void SonarQubeParser_LoadCoverage_InvalidBranchCoverage1_ThrowsCoverageParseException() {
        CoberturaParser coberturaParser = new("InvalidBranchCoverage1.xml", _directory);

        Assert.Throws<CoverageParseException>(() => coberturaParser.LoadCoverage());
    }

    [Test]
    public void SonarQubeParser_LoadCoverage_InvalidBranchCoverage2_ThrowsCoverageParseException() {
        CoberturaParser coberturaParser = new("InvalidBranchCoverage2.xml", _directory);

        Assert.Throws<CoverageParseException>(() => coberturaParser.LoadCoverage());
    }

    [Test]
    public void SonarQubeParser_LoadCoverage_InvalidBranchCoverage3_ThrowsCoverageParseException() {
        CoberturaParser coberturaParser = new("InvalidBranchCoverage3.xml", _directory);

        Assert.Throws<CoverageParseException>(() => coberturaParser.LoadCoverage());
    }

    [Test]
    public void SonarQubeParser_LoadCoverage_EmptyFile_ThrowsCoverageParseException() {
        CoberturaParser coberturaParser = new("EmptyFile.xml", _directory);

        Assert.Throws<CoverageParseException>(() => coberturaParser.LoadCoverage());
    }
}