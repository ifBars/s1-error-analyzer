using ErrorAnalyzer.Core.Presentation;

#pragma warning disable CS1591

namespace ErrorAnalyzer.Core.Models;

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

#pragma warning restore CS1591
