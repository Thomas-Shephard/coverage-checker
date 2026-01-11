using CoverageChecker.Utils;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoverageChecker.Services;

internal partial class FileFinder : IFileFinder
{
    private readonly Matcher _matcher;
    private readonly ILogger<FileFinder> _logger;

    public FileFinder(Matcher matcher, ILogger<FileFinder>? logger = null)
    {
        _matcher = matcher;
        _logger = logger ?? NullLogger<FileFinder>.Instance;
    }

    public FileFinder(IEnumerable<string> globPatterns, ILogger<FileFinder>? logger = null)
    {
        _logger = logger ?? NullLogger<FileFinder>.Instance;
        _matcher = new Matcher().AddGlobPatterns(globPatterns);
    }

    public IEnumerable<string> FindFiles(string directory)
    {
        LogSearchingForFiles(directory);
        string[] results = _matcher.GetResultsInFullPath(directory)
                                   .Select(path => PathUtils.GetNormalizedFullPath(path))
                                   .ToArray();
        LogFoundFiles(results.Length);
        return results;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Searching for files in {Directory}...")]
    private partial void LogSearchingForFiles(string directory);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Found {Count} files matching patterns.")]
    private partial void LogFoundFiles(int count);
}
