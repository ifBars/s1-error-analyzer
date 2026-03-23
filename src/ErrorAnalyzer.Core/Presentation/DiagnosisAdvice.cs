namespace ErrorAnalyzer.Core;

/// <summary>
/// UI-friendly presentation metadata attached to a diagnosis.
/// </summary>
public sealed record DiagnosisAdvice(
    string GroupKey,
    int Priority,
    string Urgency,
    string Title,
    string PrimaryAction,
    string Explanation);
