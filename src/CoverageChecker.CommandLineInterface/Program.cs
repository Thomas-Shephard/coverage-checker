using CommandLine;
using CoverageChecker.CommandLineInterface;
using Microsoft.Extensions.Logging;

ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
ILogger logger = loggerFactory.CreateLogger<Program>();

if (args.Length is 0) logger.LogInformation("No input arguments provided");
else logger.LogInformation("Arguments:{NewLine}{Arguments}", Environment.NewLine, string.Join(' ', args));

Parser.Default.ParseArguments<CliArguments>(args)
      .ThrowOnParseError(logger)
      .LogParsedArguments(logger)
      .WithParsed(cliArguments => new CoverageAnalyzer(cliArguments, loggerFactory).AnalyzeAsync());