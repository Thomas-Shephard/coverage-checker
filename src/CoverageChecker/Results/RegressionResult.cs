namespace CoverageChecker.Results;

public record RegressionResult
{
    public IReadOnlyList<RegressedFile> RegressedFiles { get; }

    public bool HasRegressions { get; }

    public RegressionResult(IEnumerable<RegressedFile> regressedFiles)
    {
        RegressedFiles = regressedFiles.ToList().AsReadOnly();
        HasRegressions = RegressedFiles.Count > 0;
    }
}
