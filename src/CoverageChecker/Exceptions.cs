namespace CoverageChecker;

/// <summary>
/// Base class for all custom exceptions thrown by this package.
/// </summary>
public abstract class CoverageException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageException"/> class.
    /// </summary>
    protected CoverageException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    protected CoverageException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    protected CoverageException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when no coverage files are found during analysis.
/// </summary>
public class NoCoverageFilesFoundException : CoverageException
{
    internal NoCoverageFilesFoundException() { }
}

/// <summary>
/// Thrown when a coverage calculation cannot be performed.
/// </summary>
public class CoverageCalculationException : CoverageException
{
    internal CoverageCalculationException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a coverage file cannot be parsed.
/// </summary>
public class CoverageParseException : CoverageException
{
    internal CoverageParseException(string message) : base(message) { }
    internal CoverageParseException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when an error occurs while interacting with Git.
/// </summary>
public class GitException : CoverageException
{
    internal GitException(string message) : base(message) { }
    internal GitException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when an error occurs while executing or waiting for a system process.
/// </summary>
public class ProcessExecutionException : CoverageException
{
    internal ProcessExecutionException(string message) : base(message) { }
}