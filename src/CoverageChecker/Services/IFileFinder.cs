namespace CoverageChecker.Services;

internal interface IFileFinder
{
    IEnumerable<string> FindFiles(string directory);
}
