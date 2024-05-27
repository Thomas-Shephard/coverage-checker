namespace CoverageChecker;

public class CoverageException(string message) : Exception(message);

public class CoverageCalculationException(string message) : CoverageException(message);