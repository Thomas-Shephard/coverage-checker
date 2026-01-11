using CoverageChecker.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoverageChecker.Services;

internal partial class CoverageMergeService(ILogger<CoverageMergeService>? logger = null) : ICoverageMergeService
{
    private readonly ILogger<CoverageMergeService> _logger = logger ?? NullLogger<CoverageMergeService>.Instance;

    public void Merge(LineCoverage existing, LineCoverage incoming)
    {
        if (!MergeRequired(existing, incoming)) return;

        existing.IsCovered = existing.IsCovered || incoming.IsCovered;
        existing.Branches ??= incoming.Branches;
        existing.ClassName ??= incoming.ClassName;
        existing.MethodName ??= incoming.MethodName;
        existing.MethodSignature ??= incoming.MethodSignature;

        if (existing.Branches is not null)
        {
            // Use the max covered branches from either, defaulting to 0 if null
            existing.CoveredBranches = Math.Max(existing.CoveredBranches ?? 0, incoming.CoveredBranches ?? 0);
        }
    }

    private bool MergeRequired(LineCoverage existing, LineCoverage incoming)
    {
        if (ReferenceEquals(existing, incoming))
        {
            return false;
        }

        if (existing.LineNumber != incoming.LineNumber)
        {
            LogMergeMismatch("line number", existing.LineNumber, incoming.LineNumber);
            throw new CoverageParseException("Cannot merge lines due to a line number mismatch");
        }

        ValidateMetadata(existing.ClassName, incoming.ClassName, "class name");
        ValidateMetadata(existing.MethodName, incoming.MethodName, "method name");
        ValidateMetadata(existing.MethodSignature, incoming.MethodSignature, "method signature");

        // If the updateable information is the same, no merge is required
        if (existing.IsCovered == incoming.IsCovered && existing.Branches == incoming.Branches && existing.CoveredBranches == incoming.CoveredBranches &&
            existing.ClassName == incoming.ClassName && existing.MethodName == incoming.MethodName && existing.MethodSignature == incoming.MethodSignature)
        {
            return false;
        }

        // If the branches are the same, no additional checks are required and a merge can be performed
        if (existing.Branches == incoming.Branches)
        {
            return true;
        }

        // Allow updating from null if the incoming line provides branch information
        if (existing.Branches is null && incoming.Branches is not null)
        {
            return true;
        }

        // If the incoming branches is null and was not covered and the line was previously covered, this is valid but
        // no update is required because it could only decrease code coverage
        if (incoming.Branches is null && !incoming.IsCovered && existing.IsCovered)
        {
            return false;
        }

        LogMergeMismatch("branches", existing.Branches, incoming.Branches);
        throw new CoverageParseException("Cannot merge lines due to a branches mismatch");
    }

    private void ValidateMetadata(string? existing, string? incoming, string label)
    {
        if (existing == null || incoming == null || existing == incoming) return;
        LogMergeMismatch(label, existing, incoming);
        throw new CoverageParseException($"Cannot merge lines due to a {label} mismatch");
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Cannot merge lines due to a {Reason} mismatch. Existing: {Existing}, Incoming: {Incoming}")]
    private partial void LogMergeMismatch(string reason, object? existing, object? incoming);
}