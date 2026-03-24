#pragma warning disable CS1591

namespace ErrorAnalyzer.Core.Models;

/// <summary>
/// Serializable grouped advice summary for browser, API, and bot consumers.
/// </summary>
public sealed class DiagnosisAdviceGroupDto
{
    public DiagnosisAdviceGroupDto()
    {
        GroupKey = string.Empty;
        Urgency = string.Empty;
        Title = string.Empty;
        PrimaryAction = string.Empty;
        Explanation = string.Empty;
        AffectedMods = Array.Empty<string>();
    }

    public DiagnosisAdviceGroupDto(
        string groupKey,
        int priority,
        string urgency,
        string title,
        string primaryAction,
        string explanation,
        IReadOnlyList<string> affectedMods,
        int diagnosisCount,
        int totalOccurrences)
    {
        GroupKey = groupKey;
        Priority = priority;
        Urgency = urgency;
        Title = title;
        PrimaryAction = primaryAction;
        Explanation = explanation;
        AffectedMods = affectedMods;
        DiagnosisCount = diagnosisCount;
        TotalOccurrences = totalOccurrences;
    }

    public string GroupKey { get; set; }

    public int Priority { get; set; }

    public string Urgency { get; set; }

    public string Title { get; set; }

    public string PrimaryAction { get; set; }

    public string Explanation { get; set; }

    public IReadOnlyList<string> AffectedMods { get; set; }

    public int DiagnosisCount { get; set; }

    public int TotalOccurrences { get; set; }
}

#pragma warning restore CS1591
