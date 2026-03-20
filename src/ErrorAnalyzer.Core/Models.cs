namespace ErrorAnalyzer.Core;

public enum RuntimeKind
{
    Unknown,
    Mono,
    Il2Cpp,
}

public enum DiagnosisSeverity
{
    Info,
    Warning,
    Error,
}

public enum DiagnosisConfidence
{
    Low,
    Medium,
    High,
}

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
    int OccurrenceCount = 1)
{
    public string Fingerprint => $"{RuleId}|{ModName}|{Evidence}";
}

public sealed record LogAnalysisResult(
    string SourceName,
    RuntimeKind Runtime,
    IReadOnlyList<Diagnosis> Diagnoses);

public sealed record AnalysisProgress(
    string Phase,
    double Progress);

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
    int OccurrenceCount);

public sealed record LogAnalysisResultDto(
    string SourceName,
    string Runtime,
    IReadOnlyList<DiagnosisDto> Diagnoses);
