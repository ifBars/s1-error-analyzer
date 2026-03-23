#pragma warning disable CS1591

namespace ErrorAnalyzer.Core.Models;

/// <summary>
/// Serializable log analysis result for API and browser consumers.
/// </summary>
public sealed class LogAnalysisResultDto
{
    public LogAnalysisResultDto()
    {
        Runtime = string.Empty;
        Diagnoses = Array.Empty<DiagnosisDto>();
    }

    public LogAnalysisResultDto(string runtime, IReadOnlyList<DiagnosisDto> diagnoses)
    {
        Runtime = runtime;
        Diagnoses = diagnoses;
    }

    public string Runtime { get; set; }

    public IReadOnlyList<DiagnosisDto> Diagnoses { get; set; }
}

#pragma warning restore CS1591
