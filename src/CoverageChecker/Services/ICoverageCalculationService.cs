using CoverageChecker.Results;

namespace CoverageChecker.Services;

public interface ICoverageCalculationService
{
    double CalculateCoverage(IEnumerable<ICoverageResult> coverageResults, CoverageType coverageType = CoverageType.Line);
}