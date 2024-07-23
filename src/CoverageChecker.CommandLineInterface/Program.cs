using CommandLine;
using CoverageChecker.CommandLineInterface;
using Microsoft.Extensions.Logging;

ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
ILogger logger = loggerFactory.CreateLogger<Program>();

if (args.Length is 0) logger.LogInformation("No input arguments provided");
else logger.LogInformation("Input arguments: {Arguments}", string.Join(' ', args));

await Parser.Default.ParseArguments<CliArguments>(args)
            .ThrowOnParseError(logger)
            .LogParsedArguments(logger)
            .WithParsedAsync(cliArguments => new CoverageAnalyzer(cliArguments, loggerFactory).AnalyzeAsync());