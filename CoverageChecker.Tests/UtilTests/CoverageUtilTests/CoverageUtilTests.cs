using CoverageChecker.Results;
using CoverageChecker.Utils;

namespace CoverageChecker.Tests.UtilTests.CoverageUtilTests;

public class CoverageUtilTests {
    [Test]
    public void CalculateCoverageUtils_Invalid_FileCoverage_ThrowsException() {
        FileCoverage[] files = [
            new FileCoverage([
                new LineCoverage(true),
                new LineCoverage(true)
            ]),
            new FileCoverage([
                new LineCoverage(true),
                new LineCoverage(true),
                new LineCoverage(true)
            ])
        ];

        Assert.Throws<ArgumentOutOfRangeException>(() => files.CalculateCoverage((CoverageType) 2));
    }

    [Test]
    public void CalculateCoverageUtils_Invalid_LineCoverage_ThrowsException() {
        LineCoverage[] lines = [
            new LineCoverage(true),
            new LineCoverage(true),
            new LineCoverage(true),
            new LineCoverage(true),
            new LineCoverage(true)
        ];

        Assert.Throws<ArgumentOutOfRangeException>(() => lines.CalculateCoverage((CoverageType) 3));
    }
}