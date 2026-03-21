namespace ErrorAnalyzer.Core;

public static class LogAnalysisResultMapper
{
    public static LogAnalysisResultDto ToDto(LogAnalysisResult result)
    {
        return new LogAnalysisResultDto(
            result.SourceName,
            result.Runtime.ToString(),
            result.Diagnoses.Select(ToDto).ToArray());
    }

    private static DiagnosisDto ToDto(Diagnosis diagnosis)
    {
        return new DiagnosisDto(
            diagnosis.RuleId,
            diagnosis.Title,
            diagnosis.Message,
            diagnosis.SuggestedAction,
            diagnosis.ModName,
            diagnosis.Evidence,
            diagnosis.LineNumber,
            diagnosis.Severity.ToString(),
            diagnosis.Confidence.ToString(),
            diagnosis.OccurrenceCount,
            diagnosis.Advice);
    }
}
