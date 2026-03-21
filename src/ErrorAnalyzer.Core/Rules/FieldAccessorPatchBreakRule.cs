namespace ErrorAnalyzer.Core;

internal sealed class FieldAccessorPatchBreakRule : IDetectionRule
{
    public IEnumerable<Diagnosis> Analyze(LogDocument document)
    {
        if (document.Runtime != RuntimeKind.Il2Cpp)
        {
            yield break;
        }

        foreach (var line in document.Lines)
        {
            if (!line.Text.Contains("Failed to init IL2CPP patch backend", StringComparison.Ordinal) ||
                !line.Text.Contains("field accessor, it can't be patched", StringComparison.Ordinal))
            {
                continue;
            }

            yield return new Diagnosis(
                RuleIds.FieldAccessorPatchBreak,
                "This mod is outdated",
                "This mod is patching game code that changed shape after an update.",
                "Update this mod if there is a newer version. If not, remove it for now.",
                document.FindNearestModName(line.Number - 1, allowForwardSearch: false),
                line.Text.Trim(),
                line.Number,
                DiagnosisSeverity.Error,
                DiagnosisConfidence.High,
                DiagnosisAdviceFactory.OutdatedMod());
        }
    }
}
