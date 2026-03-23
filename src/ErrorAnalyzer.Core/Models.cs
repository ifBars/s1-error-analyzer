using System.Security.Cryptography;
using System.Text;

namespace ErrorAnalyzer.Core;

/// <summary>
/// Identifies which runtime flavor the analyzed log appears to be using.
/// </summary>
public enum RuntimeKind
{
    /// <summary>
    /// The analyzer could not determine the runtime from the supplied log.
    /// </summary>
    Unknown,

    /// <summary>
    /// The log appears to come from the Mono runtime.
    /// </summary>
    Mono,

    /// <summary>
    /// The log appears to come from the IL2CPP runtime.
    /// </summary>
    Il2Cpp,
}

/// <summary>
/// Describes how severe a diagnosis is for the caller.
/// </summary>
public enum DiagnosisSeverity
{
    /// <summary>
    /// Informational finding with no immediate failure implied.
    /// </summary>
    Info,

    /// <summary>
    /// Warning-level finding that may explain instability or incompatibility.
    /// </summary>
    Warning,

    /// <summary>
    /// Error-level finding that is likely to break the mod or runtime.
    /// </summary>
    Error,
}

/// <summary>
/// Indicates how confident the analyzer is in a diagnosis.
/// </summary>
public enum DiagnosisConfidence
{
    /// <summary>
    /// Weak signal that may need manual verification.
    /// </summary>
    Low,

    /// <summary>
    /// Strong enough signal for a likely match.
    /// </summary>
    Medium,

    /// <summary>
    /// Highly specific match with little ambiguity.
    /// </summary>
    High,
}

/// <summary>
/// Represents a single diagnosis found while analyzing a log.
/// </summary>
public sealed record Diagnosis(
    string RuleId,
    string Title,
    string Message,
    string SuggestedAction,
    string? ModName,
    string Evidence,
    int LineNumber,
    DiagnosisSeverity Severity,
    DiagnosisConfidence Confidence,
    DiagnosisAdvice Advice,
    int OccurrenceCount = 1)
{
    /// <summary>
    /// Gets a stable identifier for deduplicating repeated findings.
    /// </summary>
    public string Fingerprint => BuildFingerprint(RuleId, ModName, Evidence);

    private static string BuildFingerprint(string ruleId, string? modName, string evidence)
    {
        var normalizedModName = modName ?? string.Empty;
        var payload = $"{ruleId.Length}:{ruleId}|{normalizedModName.Length}:{normalizedModName}|{evidence.Length}:{evidence}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
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

/// <summary>
/// Contains the completed analysis result for a single log source.
/// </summary>
public sealed record LogAnalysisResult(
    string SourceName,
    RuntimeKind Runtime,
    IReadOnlyList<Diagnosis> Diagnoses);

/// <summary>
/// Reports incremental progress while analysis is running.
/// </summary>
public sealed record AnalysisProgress(
    string Phase,
    double Progress);

/// <summary>
/// Serializable diagnosis shape for API and browser consumers.
/// </summary>
public sealed record DiagnosisDto(
    string RuleId,
    string Title,
    string Message,
    string SuggestedAction,
    string? ModName,
    string Evidence,
    int LineNumber,
    string Severity,
    string Confidence,
    int OccurrenceCount,
    DiagnosisAdvice Advice);

/// <summary>
/// Serializable log analysis result for API and browser consumers.
/// </summary>
public sealed record LogAnalysisResultDto(
    string SourceName,
    string Runtime,
    IReadOnlyList<DiagnosisDto> Diagnoses);
