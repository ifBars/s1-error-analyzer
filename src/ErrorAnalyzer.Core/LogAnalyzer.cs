using System.Threading.Tasks;

namespace ErrorAnalyzer.Core;

public sealed class LogAnalyzer
{
    private readonly IReadOnlyList<IDetectionRule> _rules;
    private readonly DiagnosisAggregator _aggregator = new();
    private const double RuleProgressStart = 0.2;
    private const double RuleProgressRange = 0.68;

    public LogAnalyzer()
        : this(new IDetectionRule[]
        {
            new DualRuntimeInstallRule(),
            new MissingPatchTargetRule(),
            new MissingMethodRule(),
            new MissingDependencyRule(),
            new RuntimeMismatchMonoModOnIl2CppRule(),
            new OutdatedTypeReferenceRule(),
            new FieldAccessorPatchBreakRule(),
        })
    {
    }

    internal LogAnalyzer(IReadOnlyList<IDetectionRule> rules)
    {
        _rules = rules;
    }

    public LogAnalysisResult AnalyzeText(string text, string sourceName, Action<AnalysisProgress>? reportProgress = null)
    {
        reportProgress?.Invoke(new AnalysisProgress("Parsing log", 0.05));
        var document = new LogDocument(sourceName, text);
        reportProgress?.Invoke(new AnalysisProgress("Checking runtime markers", 0.14));

        var diagnoses = new List<Diagnosis>();
        for (var index = 0; index < _rules.Count; index++)
        {
            var rule = _rules[index];
            reportProgress?.Invoke(new AnalysisProgress(GetRulePhase(rule), RuleProgressStart + (RuleProgressRange * index / _rules.Count)));
            diagnoses.AddRange(rule.Analyze(document));
        }

        reportProgress?.Invoke(new AnalysisProgress("Aggregating findings", 0.92));
        var aggregatedDiagnoses = _aggregator.Aggregate(diagnoses);
        reportProgress?.Invoke(new AnalysisProgress("Finalizing report", 0.98));

        return new LogAnalysisResult(sourceName, document.Runtime, aggregatedDiagnoses);
    }

    public LogAnalysisResult AnalyzeFile(string path)
        => AnalyzeText(File.ReadAllText(path), Path.GetFileName(path));

    public LogAnalysisResultDto AnalyzeTextAsDto(string text, string sourceName)
        => LogAnalysisResultMapper.ToDto(AnalyzeText(text, sourceName));

    public LogAnalysisResultDto AnalyzeTextAsDto(string text, string sourceName, Action<AnalysisProgress>? reportProgress)
        => LogAnalysisResultMapper.ToDto(AnalyzeText(text, sourceName, reportProgress));

    public async Task<LogAnalysisResultDto> AnalyzeTextAsDtoAsync(
        string text,
        string sourceName,
        Func<AnalysisProgress, Task>? reportProgress)
    {
        if (reportProgress is null)
        {
            return AnalyzeTextAsDto(text, sourceName);
        }

        await reportProgress(new AnalysisProgress("Parsing log", 0.05));
        var document = new LogDocument(sourceName, text);
        await reportProgress(new AnalysisProgress("Checking runtime markers", 0.14));

        var diagnoses = new List<Diagnosis>();
        for (var index = 0; index < _rules.Count; index++)
        {
            var rule = _rules[index];
            await reportProgress(new AnalysisProgress(GetRulePhase(rule), RuleProgressStart + (RuleProgressRange * index / _rules.Count)));
            diagnoses.AddRange(rule.Analyze(document));
        }

        await reportProgress(new AnalysisProgress("Aggregating findings", 0.92));
        var aggregatedDiagnoses = _aggregator.Aggregate(diagnoses);
        await reportProgress(new AnalysisProgress("Finalizing report", 0.98));

        return LogAnalysisResultMapper.ToDto(new LogAnalysisResult(sourceName, document.Runtime, aggregatedDiagnoses));
    }

    public LogAnalysisResultDto AnalyzeFileAsDto(string path)
        => LogAnalysisResultMapper.ToDto(AnalyzeFile(path));

    private static string GetRulePhase(IDetectionRule rule)
    {
        return rule switch
        {
            DualRuntimeInstallRule => "Checking duplicate mod installs",
            MissingPatchTargetRule => "Checking Harmony patch targets",
            MissingMethodRule => "Checking missing methods",
            MissingDependencyRule => "Checking missing dependencies",
            RuntimeMismatchMonoModOnIl2CppRule => "Checking runtime mismatches",
            OutdatedTypeReferenceRule => "Checking outdated type references",
            FieldAccessorPatchBreakRule => "Checking IL2CPP patch breakage",
            _ => "Checking analyzer rules",
        };
    }
}

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
            diagnosis.OccurrenceCount);
    }
}
