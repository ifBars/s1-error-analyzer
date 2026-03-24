using ErrorAnalyzer.Core.Models;
using ErrorAnalyzer.Core.Presentation;
using ErrorAnalyzer.Core.Parsing;
using ErrorAnalyzer.Core.Runtime;

namespace ErrorAnalyzer.Core.Rules;

internal sealed class RuntimeMismatchMonoModOnIl2CppRule : IDetectionRule
{
    public IEnumerable<Diagnosis> Analyze(LogDocument document)
    {
        if (document.Runtime != RuntimeKind.Il2Cpp)
        {
            yield break;
        }

        var conflictModKeys = RuntimeVariantDetector.FindConflictModKeys(document);
        foreach (var line in document.Lines)
        {
            if (!line.Text.Contains("Could not load type 'ScheduleOne.", StringComparison.Ordinal) ||
                !line.Text.Contains("from assembly 'Assembly-CSharp", StringComparison.Ordinal))
            {
                continue;
            }

            var modName = document.FindNearestModName(line.Number - 1);
            if (conflictModKeys.Contains(ModNameNormalizer.GetEquivalenceKey(modName)))
            {
                continue;
            }

            yield return new Diagnosis(
                RuleIds.RuntimeMismatchMonoModOnIl2Cpp,
                "Wrong version of the mod is installed",
                "This looks like the wrong build of the mod for the current version of the game.",
                "Look for a version of this mod labeled `Il2Cpp`. If you cannot find one, remove this mod.",
                modName,
                line.Text.Trim(),
                line.Number,
                DiagnosisSeverity.Error,
                DiagnosisConfidence.High,
                DiagnosisAdviceFactory.WrongRuntimeBuild());
        }
    }
}
