using CoverageChecker.Parsers;
using CoverageChecker.Results;
using CoverageChecker.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoverageChecker.Tests.Unit.ParserTests;

public class ParserFactoryTests
{
    private ParserFactory _factory;

    [SetUp]
    public void SetUp()
    {
        _factory = new ParserFactory(new CoverageMergeService());
    }

    [Test]
    public void ParserFactoryCoberturaCoverageFormatReturnsCoberturaParser()
    {
        ICoverageParser parser = _factory.CreateParser(CoverageFormat.Cobertura, new Coverage(), NullLoggerFactory.Instance);

        Assert.That(parser, Is.InstanceOf<CoberturaParser>());
    }

    [Test]
    public void ParserFactorySonarQubeCoverageFormatReturnsSonarQubeParser()
    {
        ICoverageParser parser = _factory.CreateParser(CoverageFormat.SonarQube, new Coverage(), NullLoggerFactory.Instance);

        Assert.That(parser, Is.InstanceOf<SonarQubeParser>());
    }

    [Test]
    public void ParserFactoryAutoCoverageFormatThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _factory.CreateParser(CoverageFormat.Auto, new Coverage(), NullLoggerFactory.Instance));
    }

    [Test]
    public void ParserFactoryUnknownCoverageFormatThrowsException()
    {
        const CoverageFormat coverageFormat = (CoverageFormat)99;

        Exception e = Assert.Throws<ArgumentOutOfRangeException>(() => _factory.CreateParser(coverageFormat, new Coverage(), NullLoggerFactory.Instance));
        Assert.That(e.Message, Does.Contain("Unknown or unsupported coverage format"));
    }

    [Test]
    public void DetectFormatCoberturaFileReturnsCobertura()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "<?xml version=\"1.0\"?><coverage line-rate=\"0.5\"></coverage>");
            Assert.That(_factory.DetectFormat(path), Is.EqualTo(CoverageFormat.Cobertura));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void DetectFormatSonarQubeFileReturnsSonarQube()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "<coverage version=\"1\"><file path=\"test.cs\"></file></coverage>");
            Assert.That(_factory.DetectFormat(path), Is.EqualTo(CoverageFormat.SonarQube));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void DetectFormatUnknownFileThrowsCoverageParseException()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "<unknown></unknown>");
            Assert.Throws<CoverageParseException>(() => _factory.DetectFormat(path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}