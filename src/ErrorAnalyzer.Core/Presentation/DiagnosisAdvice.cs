#pragma warning disable CS1591

namespace ErrorAnalyzer.Core.Presentation;

/// <summary>
/// UI-friendly presentation metadata attached to a diagnosis.
/// </summary>
public sealed class DiagnosisAdvice
{
    public DiagnosisAdvice()
    {
        GroupKey = string.Empty;
        Urgency = string.Empty;
        Title = string.Empty;
        PrimaryAction = string.Empty;
        Explanation = string.Empty;
    }

    public DiagnosisAdvice(
        string groupKey,
        int priority,
        string urgency,
        string title,
        string primaryAction,
        string explanation)
    {
        GroupKey = groupKey;
        Priority = priority;
        Urgency = urgency;
        Title = title;
        PrimaryAction = primaryAction;
        Explanation = explanation;
    }

    public string GroupKey { get; set; }

    public int Priority { get; set; }

    public string Urgency { get; set; }

    public string Title { get; set; }

    public string PrimaryAction { get; set; }

    public string Explanation { get; set; }
}

#pragma warning restore CS1591
