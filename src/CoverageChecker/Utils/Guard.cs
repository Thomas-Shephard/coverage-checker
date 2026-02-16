namespace CoverageChecker.Utils;

internal static class Guard
{
    public static double ValidateThreshold(double value, string paramName)
    {
        if (double.IsNaN(value) || value < 0 || value > 1)
        {
            throw new ArgumentOutOfRangeException(paramName, $"{paramName} must be between 0.0 and 1.0.");
        }

        return value;
    }
}
