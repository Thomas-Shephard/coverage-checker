using CoverageChecker.Parsers;
using CoverageChecker.Results;
using CoverageChecker.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoverageChecker.Tests.Unit.ParserTests;

public class ParserFactoryTests
{
    [Test]
    public void ParserFactoryCoberturaCoverageFormatReturnsCoberturaParser()
    {
        ICoverageParser parser = new ParserFactory(new CoverageMergeService()).CreateParser(CoverageFormat.Cobertura, new Coverage(), NullLoggerFactory.Instance);

        Assert.That(parser, Is.InstanceOf<CoberturaParser>());
    }

    [Test]
    public void ParserFactorySonarQubeCoverageFormatReturnsSonarQubeParser()
    {
        ICoverageParser parser = new ParserFactory(new CoverageMergeService()).CreateParser(CoverageFormat.SonarQube, new Coverage(), NullLoggerFactory.Instance);

        Assert.That(parser, Is.InstanceOf<SonarQubeParser>());
    }

    [Test]
    public void ParserFactoryUnknownCoverageFormatThrowsException()
    {
        const CoverageFormat coverageFormat = (CoverageFormat)(-1);

        Exception e = Assert.Throws<ArgumentOutOfRangeException>(() => new ParserFactory(new CoverageMergeService()).CreateParser(coverageFormat, new Coverage(), NullLoggerFactory.Instance));
        Assert.That(e.Message, Is.EqualTo($"Unknown coverage format (Parameter '{nameof(coverageFormat)}')"));
    }
}