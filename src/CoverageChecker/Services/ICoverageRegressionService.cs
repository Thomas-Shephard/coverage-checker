using CoverageChecker.Results;

namespace CoverageChecker.Services;

internal interface ICoverageRegressionService
{
    RegressionResult CheckRegression(Coverage baseline, Coverage current, double epsilon = CoverageAnalyser.DefaultEpsilon);
}
