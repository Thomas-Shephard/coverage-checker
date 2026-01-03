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

        if (existing.Branches is not null)
        {
             // Use the max covered branches from either, defaulting to 0 if null
            existing.CoveredBranches = Math.Max(existing.CoveredBranches ?? 0, incoming.CoveredBranches ?? 0);
        }
        existing.ClassName ??= incoming.ClassName;
        existing.MethodName ??= incoming.MethodName;
        existing.MethodSignature ??= incoming.MethodSignature;
    }

    private bool MergeRequired(LineCoverage existing, LineCoverage incoming)
    {
        if (ReferenceEquals(existing, incoming))
        {
            return false;
        }

        if (existing.LineNumber != incoming.LineNumber)
        {
            LogMergeMismatch("line number", existing.LineNumber.ToString(), incoming.LineNumber.ToString());
            throw new CoverageParseException("Cannot merge lines due to a line number mismatch");
        }

        if (existing.ClassName != null && incoming.ClassName != null && existing.ClassName != incoming.ClassName)
        {
            LogMergeMismatch("class name", existing.ClassName, incoming.ClassName);
            throw new CoverageParseException("Cannot merge lines due to a class name mismatch");
        }

        if (existing.MethodName != null && incoming.MethodName != null && existing.MethodName != incoming.MethodName)
        {
            LogMergeMismatch("method name", existing.MethodName, incoming.MethodName);
            throw new CoverageParseException("Cannot merge lines due to a method name mismatch");
        }

        if (existing.MethodSignature != null && incoming.MethodSignature != null && existing.MethodSignature != incoming.MethodSignature)
        {
            LogMergeMismatch("method signature", existing.MethodSignature, incoming.MethodSignature);
            throw new CoverageParseException("Cannot merge lines due to a method signature mismatch");
        }

        // If the updateable information is the same, no merge is required
        if (existing.IsCovered == incoming.IsCovered && 
            existing.Branches == incoming.Branches && 
            existing.CoveredBranches == incoming.CoveredBranches &&
            existing.ClassName == incoming.ClassName &&
            existing.MethodName == incoming.MethodName &&
            existing.MethodSignature == incoming.MethodSignature)
        {
            return false;
        }

        // If metadata is being updated, merge is required
        if (existing.ClassName != incoming.ClassName || 
            existing.MethodName != incoming.MethodName || 
            existing.MethodSignature != incoming.MethodSignature)
        {
            return true;
        }

        // If the branches are the same, no additional checks are required and a merge can be performed
        if (existing.Branches == incoming.Branches)
        {
            return true;
        }

        // Branches can only be updated from null when the line was previously not covered but now is
        if (existing.Branches is null && !existing.IsCovered && incoming.IsCovered)
        {
            return true;
        }

        // If the incoming branches is null and was not covered and the line was previously covered, this is valid but
        // no update is required because it could only decrease code coverage
        if (incoming.Branches is null && !incoming.IsCovered && existing.IsCovered)
        {
            return false;
        }

        LogMergeMismatch("branches", existing.Branches?.ToString() ?? "null", incoming.Branches?.ToString() ?? "null");
        throw new CoverageParseException("Cannot merge lines due to a branches mismatch");
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Cannot merge lines due to a {Reason} mismatch. Existing: {Existing}, Incoming: {Incoming}")]
    private partial void LogMergeMismatch(string reason, string existing, string incoming);
}