using ErrorAnalyzer.Core.Models;
using ErrorAnalyzer.Core.Presentation;
using ErrorAnalyzer.Core.Parsing;

namespace ErrorAnalyzer.Core.Rules;

internal sealed class MissingPatchTargetRule : IDetectionRule
{
    public IEnumerable<Diagnosis> Analyze(LogDocument document)
    {
        for (var index = 0; index < document.Lines.Count; index++)
        {
            var line = document.Lines[index];
            if (!line.Text.Contains("HarmonyLib.HarmonyException: Patching exception in method null", StringComparison.Ordinal))
            {
                continue;
            }

            var hasPatchTargetEvidence = document.ContainsNearby(index, 3, text =>
                text.Contains("Undefined target method", StringComparison.Ordinal) ||
                text.Contains("Could not find method", StringComparison.Ordinal));

            if (!hasPatchTargetEvidence)
            {
                continue;
            }

            yield return new Diagnosis(
                RuleIds.MissingPatchTarget,
                "This mod is outdated",
                "This mod is trying to hook into game code that changed after an update.",
                "Update this mod if there is a newer version. If not, remove it for now.",
                document.FindNearestModName(index),
                line.Text.Trim(),
                line.Number,
                DiagnosisSeverity.Error,
                DiagnosisConfidence.High,
                DiagnosisAdviceFactory.OutdatedMod());
        }
    }
}
