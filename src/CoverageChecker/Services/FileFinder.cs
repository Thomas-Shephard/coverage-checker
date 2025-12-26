using CoverageChecker.Utils;
using Microsoft.Extensions.FileSystemGlobbing;

namespace CoverageChecker.Services;

internal class FileFinder : IFileFinder
{
    private readonly Matcher _matcher;

    public FileFinder(Matcher matcher)
    {
        _matcher = matcher;
    }

    public FileFinder(IEnumerable<string> globPatterns)
    {
        globPatterns = globPatterns as string[] ?? globPatterns.ToArray();
        if (!globPatterns.Any())
        {
            throw new ArgumentException("At least one glob pattern must be provided", nameof(globPatterns));
        }

        _matcher = new Matcher().AddGlobPatterns(globPatterns);
    }

    public IEnumerable<string> FindFiles(string directory)
    {
        return _matcher.GetResultsInFullPath(directory);
    }
}
