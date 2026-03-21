namespace ErrorAnalyzer.Core;

internal sealed class MissingMethodRule : IDetectionRule
{
    public IEnumerable<Diagnosis> Analyze(LogDocument document)
    {
        foreach (var line in document.Lines)
        {
            if (!line.Text.Contains("System.MissingMethodException: Method not found", StringComparison.Ordinal))
            {
                continue;
            }

            yield return new Diagnosis(
                RuleIds.MissingMethod,
                "This mod is outdated",
                "This mod is looking for game code that is no longer there after an update.",
                "Update this mod if there is a newer version. If not, remove it for now.",
                document.FindNearestModName(line.Number - 1),
                line.Text.Trim(),
                line.Number,
                DiagnosisSeverity.Error,
                DiagnosisConfidence.High,
                DiagnosisAdviceFactory.OutdatedMod());
        }
    }
}
