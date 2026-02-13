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
    public void ParserFactory_CoberturaCoverageFormat_ReturnsCoberturaParser()
    {
        ICoverageParser parser = _factory.CreateParser(CoverageFormat.Cobertura, new Coverage(), NullLoggerFactory.Instance);

        Assert.That(parser, Is.InstanceOf<CoberturaParser>());
    }

    [Test]
    public void ParserFactory_SonarQubeCoverageFormat_ReturnsSonarQubeParser()
    {
        ICoverageParser parser = _factory.CreateParser(CoverageFormat.SonarQube, new Coverage(), NullLoggerFactory.Instance);

        Assert.That(parser, Is.InstanceOf<SonarQubeParser>());
    }

    [Test]
    public void ParserFactory_AutoCoverageFormat_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _factory.CreateParser(CoverageFormat.Auto, new Coverage(), NullLoggerFactory.Instance));
    }

    [Test]
    public void ParserFactory_UnknownCoverageFormat_ThrowsException()
    {
        const CoverageFormat coverageFormat = (CoverageFormat)99;

        Exception e = Assert.Throws<ArgumentOutOfRangeException>(() => _factory.CreateParser(coverageFormat, new Coverage(), NullLoggerFactory.Instance));
        Assert.That(e.Message, Does.Contain("Unknown or unsupported coverage format"));
    }

    [Test]
    public void DetectFormat_CoberturaFile_ReturnsCobertura()
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
    public void DetectFormat_SonarQubeFile_ReturnsSonarQube()
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
    public void DetectFormat_UnknownFile_ThrowsCoverageParseException()
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