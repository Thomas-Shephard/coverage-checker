namespace CoverageChecker.Results;

/// <summary>
/// Represents the result of a coverage regression analysis.
/// </summary>
public record RegressionResult
{
    /// <summary>
    /// Gets the list of files that have regressed in coverage.
    /// </summary>
    public IReadOnlyList<RegressedFile> RegressedFiles { get; }

    /// <summary>
    /// Gets a value indicating whether any regressions were found.
    /// </summary>
    public bool HasRegressions { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RegressionResult"/> class.
    /// </summary>
    /// <param name="regressedFiles">The list of regressed files.</param>
    public RegressionResult(IEnumerable<RegressedFile>? regressedFiles)
    {
        RegressedFiles = (regressedFiles?.ToList() ?? []).AsReadOnly();
        HasRegressions = RegressedFiles.Count > 0;
    }
}
