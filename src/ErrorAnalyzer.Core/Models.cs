using System.Security.Cryptography;
using System.Text;
using ErrorAnalyzer.Core.Presentation;

#pragma warning disable CS1591

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
        var normalizedModName = modName ?? string.Empty;
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

/// <summary>
/// Contains the completed analysis result for a single log source.
/// </summary>
public sealed class LogAnalysisResult
{
    public LogAnalysisResult()
    {
        SourceName = string.Empty;
        Diagnoses = Array.Empty<Diagnosis>();
    }

    public LogAnalysisResult(string sourceName, RuntimeKind runtime, IReadOnlyList<Diagnosis> diagnoses)
    {
        SourceName = sourceName;
        Runtime = runtime;
        Diagnoses = diagnoses;
    }

    public string SourceName { get; set; }

    public RuntimeKind Runtime { get; set; }

    public IReadOnlyList<Diagnosis> Diagnoses { get; set; }
}

/// <summary>
/// Reports incremental progress while analysis is running.
/// </summary>
public sealed class AnalysisProgress
{
    public AnalysisProgress()
    {
        Phase = string.Empty;
    }

    public AnalysisProgress(string phase, double progress)
    {
        Phase = phase;
        Progress = progress;
    }

    public string Phase { get; set; }

    public double Progress { get; set; }
}

/// <summary>
/// Serializable diagnosis shape for API and browser consumers.
/// </summary>
public sealed class DiagnosisDto
{
    public DiagnosisDto()
    {
        RuleId = string.Empty;
        Title = string.Empty;
        Message = string.Empty;
        SuggestedAction = string.Empty;
        Evidence = string.Empty;
        Severity = string.Empty;
        Confidence = string.Empty;
        Advice = new DiagnosisAdvice();
    }

    public DiagnosisDto(
        string ruleId,
        string title,
        string message,
        string suggestedAction,
        string? modName,
        string evidence,
        int lineNumber,
        string severity,
        string confidence,
        int occurrenceCount,
        DiagnosisAdvice advice)
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
        OccurrenceCount = occurrenceCount;
        Advice = advice;
    }

    public string RuleId { get; set; }

    public string Title { get; set; }

    public string Message { get; set; }

    public string SuggestedAction { get; set; }

    public string? ModName { get; set; }

    public string Evidence { get; set; }

    public int LineNumber { get; set; }

    public string Severity { get; set; }

    public string Confidence { get; set; }

    public int OccurrenceCount { get; set; }

    public DiagnosisAdvice Advice { get; set; }
}

/// <summary>
/// Serializable log analysis result for API and browser consumers.
/// </summary>
public sealed class LogAnalysisResultDto
{
    public LogAnalysisResultDto()
    {
        SourceName = string.Empty;
        Runtime = string.Empty;
        Diagnoses = Array.Empty<DiagnosisDto>();
    }

    public LogAnalysisResultDto(string sourceName, string runtime, IReadOnlyList<DiagnosisDto> diagnoses)
    {
        SourceName = sourceName;
        Runtime = runtime;
        Diagnoses = diagnoses;
    }

    public string SourceName { get; set; }

    public string Runtime { get; set; }

    public IReadOnlyList<DiagnosisDto> Diagnoses { get; set; }
}

#pragma warning restore CS1591
