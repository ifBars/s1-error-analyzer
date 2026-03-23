#pragma warning disable CS1591

namespace ErrorAnalyzer.Core.Models;

/// <summary>
/// Contains the completed analysis result for a single log source.
/// </summary>
public sealed class LogAnalysisResult
{
    public LogAnalysisResult()
    {
        Diagnoses = Array.Empty<Diagnosis>();
    }

    public LogAnalysisResult(RuntimeKind runtime, IReadOnlyList<Diagnosis> diagnoses)
    {
        Runtime = runtime;
        Diagnoses = diagnoses;
    }

    public RuntimeKind Runtime { get; set; }

    public IReadOnlyList<Diagnosis> Diagnoses { get; set; }
}

#pragma warning restore CS1591
