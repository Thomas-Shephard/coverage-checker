namespace CoverageChecker;

public class CoverageException(string message) : Exception(message);

public class CoverageCalculationException(string message) : CoverageException(message);

public class CoverageParseException(string message) : CoverageException(message);