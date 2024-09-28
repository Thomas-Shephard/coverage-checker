namespace CoverageChecker;

public abstract class CoverageException : Exception {
    protected CoverageException() { }
    protected CoverageException(string message) : base(message) { }
    protected CoverageException(string message, Exception innerException) : base(message, innerException) { }
}

public class NoCoverageFilesFoundException : CoverageException {
    internal NoCoverageFilesFoundException() { }
}

public class CoverageCalculationException : CoverageException {
    internal CoverageCalculationException(string message) : base(message) { }
}

public class CoverageParseException : CoverageException {
    internal CoverageParseException(string message) : base(message) { }
    internal CoverageParseException(string message, Exception innerException) : base(message, innerException) { }
}