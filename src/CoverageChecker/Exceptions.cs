namespace CoverageChecker;

/// <summary>
/// Base class for all custom exceptions thrown by this package.
/// </summary>
public abstract class CoverageException : Exception {
    protected CoverageException() { }
    protected CoverageException(string message) : base(message) { }
    protected CoverageException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when no coverage files are found during analysis.
/// </summary>
public class NoCoverageFilesFoundException : CoverageException {
    internal NoCoverageFilesFoundException() { }
}

/// <summary>
/// Thrown when a coverage calculation cannot be performed.
/// </summary>
public class CoverageCalculationException : CoverageException {
    internal CoverageCalculationException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a coverage file cannot be parsed.
/// </summary>
public class CoverageParseException : CoverageException {
    internal CoverageParseException(string message) : base(message) { }
    internal CoverageParseException(string message, Exception innerException) : base(message, innerException) { }
}