using ErrorAnalyzer.Core.Models;
using ErrorAnalyzer.Core.Presentation;
using ErrorAnalyzer.Core.Parsing;

namespace ErrorAnalyzer.Core.Rules;

internal sealed class OutdatedTypeReferenceRule : IDetectionRule
{
    public IEnumerable<Diagnosis> Analyze(LogDocument document)
    {
        foreach (var line in document.Lines)
        {
            var text = line.Text;
            var isTypeLoad = text.Contains("System.TypeLoadException: Could not load type '", StringComparison.Ordinal) ||
                             text.StartsWith("Could not load type '", StringComparison.Ordinal);
            if (!isTypeLoad)
            {
                continue;
            }

            if (document.Runtime == RuntimeKind.Il2Cpp &&
                text.Contains("Could not load type 'ScheduleOne.", StringComparison.Ordinal) &&
                text.Contains("from assembly 'Assembly-CSharp", StringComparison.Ordinal))
            {
                continue;
            }

            if (document.IsHarmonyAssemblyScanFailure(line.Number - 1))
            {
                continue;
            }

            yield return new Diagnosis(
                RuleIds.OutdatedTypeReference,
                "This mod is outdated",
                "This mod is trying to use game code that changed after an update.",
                "Update this mod if there is a newer version. If not, remove it for now.",
                document.FindNearestModName(line.Number - 1),
                text.Trim(),
                line.Number,
                DiagnosisSeverity.Error,
                DiagnosisConfidence.High,
                DiagnosisAdviceFactory.OutdatedMod());
        }
    }
}
