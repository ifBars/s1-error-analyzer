namespace ErrorAnalyzer.Core;

public sealed record DiagnosisAdvice(
    string GroupKey,
    int Priority,
    string Urgency,
    string Title,
    string PrimaryAction,
    string Explanation);
