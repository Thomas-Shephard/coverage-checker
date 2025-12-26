using CoverageChecker.Parsers;
using CoverageChecker.Results;

namespace CoverageChecker.Tests.Unit.ParserTests;

public class ParserFactoryTests
{
    [Test]
    public void ParserFactory_CoberturaCoverageFormat_ReturnsCoberturaParser()
    {
        ICoverageParser parser = new ParserFactory().CreateParser(CoverageFormat.Cobertura, new Coverage());

        Assert.That(parser, Is.InstanceOf<CoberturaParser>());
    }

    [Test]
    public void ParserFactory_SonarQubeCoverageFormat_ReturnsSonarQubeParser()
    {
        ICoverageParser parser = new ParserFactory().CreateParser(CoverageFormat.SonarQube, new Coverage());

        Assert.That(parser, Is.InstanceOf<SonarQubeParser>());
    }

    [Test]
    public void ParserFactory_UnknownCoverageFormat_ThrowsException()
    {
        const CoverageFormat coverageFormat = (CoverageFormat)(-1);

        Exception e = Assert.Throws<ArgumentOutOfRangeException>(() => new ParserFactory().CreateParser(coverageFormat, new Coverage()));
        Assert.That(e.Message, Is.EqualTo($"Unknown coverage format (Parameter '{nameof(coverageFormat)}')"));
    }
}