using System.Security.Cryptography;
using System.Text;
using ErrorAnalyzer.Core.Presentation;

#pragma warning disable CS1591

namespace ErrorAnalyzer.Core.Models;

/// <summary>
/// Represents a single diagnosis found while analyzing a log.
/// </summary>
public sealed class Diagnosis
{
    public Diagnosis()
    {
        RuleId = string.Empty;
        Title = string.Empty;
        Message = string.Empty;
        SuggestedAction = string.Empty;
        Evidence = string.Empty;
        Advice = new DiagnosisAdvice();
        OccurrenceCount = 1;
    }

    public Diagnosis(
        string ruleId,
        string title,
        string message,
        string suggestedAction,
        string? modName,
        string evidence,
        int lineNumber,
        DiagnosisSeverity severity,
        DiagnosisConfidence confidence,
        DiagnosisAdvice advice,
        int occurrenceCount = 1)
    {
        RuleId = ruleId;
        Title = title;
        Message = message;
        SuggestedAction = suggestedAction;
        ModName = modName;
        Evidence = evidence;
        LineNumber = lineNumber;
        Severity = severity;
        Confidence = confidence;
        Advice = advice;
        OccurrenceCount = occurrenceCount;
    }

    public string RuleId { get; set; }

    public string Title { get; set; }

    public string Message { get; set; }

    public string SuggestedAction { get; set; }

    public string? ModName { get; set; }

    public string Evidence { get; set; }

    public int LineNumber { get; set; }

    public DiagnosisSeverity Severity { get; set; }

    public DiagnosisConfidence Confidence { get; set; }

    public DiagnosisAdvice Advice { get; set; }

    public int OccurrenceCount { get; set; }

    /// <summary>
    /// Gets a stable identifier for deduplicating repeated findings.
    /// </summary>
    public string Fingerprint => BuildFingerprint(RuleId, ModName, Evidence);

    private static string BuildFingerprint(string ruleId, string? modName, string evidence)
    {
        var normalizedModName = ModNameNormalizer.GetEquivalenceKey(modName);
        var payload = $"{ruleId.Length}:{ruleId}|{normalizedModName.Length}:{normalizedModName}|{evidence.Length}:{evidence}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }

    /// <summary>
    /// Creates a diagnosis using a generated generic advice payload.
    /// </summary>
    public Diagnosis(
        string ruleId,
        string title,
        string message,
        string suggestedAction,
        string? modName,
        string evidence,
        int lineNumber,
        DiagnosisSeverity severity,
        DiagnosisConfidence confidence,
        int occurrenceCount = 1)
        : this(
              ruleId,
              title,
              message,
              suggestedAction,
              modName,
              evidence,
              lineNumber,
              severity,
              confidence,
              DiagnosisAdviceFactory.Generic(ruleId, title, message, suggestedAction),
              occurrenceCount)
    {
    }
}

#pragma warning restore CS1591
