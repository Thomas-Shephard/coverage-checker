namespace CoverageChecker;

/// <summary>
/// The format of the coverage report.
/// </summary>
public enum CoverageFormat
{
    /// <summary>
    /// Automatically detect the coverage format.
    /// </summary>
    Auto,

    /// <summary>
    /// The Cobertura coverage format.
    /// </summary>
    Cobertura,

    /// <summary>
    /// The SonarQube coverage format.
    /// </summary>
    SonarQube,

    /// <summary>
    /// The OpenCover coverage format.
    /// </summary>
    OpenCover
}
