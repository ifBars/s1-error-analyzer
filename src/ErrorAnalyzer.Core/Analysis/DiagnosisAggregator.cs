namespace ErrorAnalyzer.Core;

internal sealed class DiagnosisAggregator
{
    public IReadOnlyList<Diagnosis> Aggregate(IEnumerable<Diagnosis> diagnoses)
    {
        var aggregated = new Dictionary<string, Diagnosis>(StringComparer.Ordinal);

        foreach (var diagnosis in diagnoses)
        {
            var key = BuildAggregateKey(diagnosis);
            if (!aggregated.TryGetValue(key, out var existing))
            {
                aggregated[key] = diagnosis;
                continue;
            }

            var earliestDiagnosis = diagnosis.LineNumber < existing.LineNumber
                ? diagnosis with { OccurrenceCount = existing.OccurrenceCount + 1 }
                : existing with { OccurrenceCount = existing.OccurrenceCount + 1 };
            aggregated[key] = earliestDiagnosis;
        }

        return aggregated.Values
            .OrderByDescending(diagnosis => diagnosis.Severity)
            .ThenBy(diagnosis => diagnosis.Advice.Priority)
            .ThenByDescending(diagnosis => diagnosis.OccurrenceCount)
            .ThenBy(diagnosis => diagnosis.LineNumber)
            .ToArray();
    }

    private static string BuildAggregateKey(Diagnosis diagnosis)
        => $"{diagnosis.RuleId}|{diagnosis.ModName}|{NormalizeEvidence(diagnosis.Evidence)}";

    private static string NormalizeEvidence(string evidence)
    {
        return evidence
            .Replace("'Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'", "'Assembly-CSharp'", StringComparison.Ordinal)
            .Replace("'UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'", "'UnityEngine.CoreModule'", StringComparison.Ordinal);
    }
}
