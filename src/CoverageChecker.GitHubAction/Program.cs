using CommandLine;
using CoverageChecker.GitHubAction;

ParserResult<ActionInputs> parser = Parser.Default.ParseArguments(() => new ActionInputs(), args);

parser.WithNotParsed(_ => Environment.Exit(1));

parser.WithParsed(options =>
{
    new CoverageAnalyzer(options).AnalyzeAsync();
});