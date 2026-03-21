namespace ErrorAnalyzer.Core;

internal sealed class RuntimeMismatchMonoModOnIl2CppRule : IDetectionRule
{
    public IEnumerable<Diagnosis> Analyze(LogDocument document)
    {
        if (document.Runtime != RuntimeKind.Il2Cpp)
        {
            yield break;
        }

        foreach (var line in document.Lines)
        {
            if (!line.Text.Contains("Could not load type 'ScheduleOne.", StringComparison.Ordinal) ||
                !line.Text.Contains("from assembly 'Assembly-CSharp", StringComparison.Ordinal))
            {
                continue;
            }

            yield return new Diagnosis(
                RuleIds.RuntimeMismatchMonoModOnIl2Cpp,
                "Wrong version of the mod is installed",
                "This looks like the wrong build of the mod for the current version of the game.",
                "Look for a version of this mod labeled `Il2Cpp`. If you cannot find one, remove this mod.",
                document.FindNearestModName(line.Number - 1),
                line.Text.Trim(),
                line.Number,
                DiagnosisSeverity.Error,
                DiagnosisConfidence.High,
                new DiagnosisAdvice(
                    RuleIds.RuntimeMismatchMonoModOnIl2Cpp,
                    2,
                    "Most likely fix",
                    "You have the wrong version of a mod installed",
                    "Look for a version of this mod that says Il2Cpp. If you cannot find one, remove the mod.",
                    "The installed copy was built for a different game setup."));
        }
    }
}
