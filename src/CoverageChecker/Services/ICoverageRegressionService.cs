using CoverageChecker.Results;

namespace CoverageChecker.Services;

internal interface ICoverageRegressionService
{
    RegressionResult CheckRegression(Coverage baseline, Coverage current, IDictionary<string, string>? renames = null, double epsilon = CoverageAnalyser.DefaultEpsilon);
}
