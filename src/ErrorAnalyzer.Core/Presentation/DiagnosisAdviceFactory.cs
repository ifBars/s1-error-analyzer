using ErrorAnalyzer.Core.Rules;

namespace ErrorAnalyzer.Core.Presentation;

/// <summary>
/// Creates standardized advice payloads for diagnoses.
/// </summary>
public static class DiagnosisAdviceFactory
{
    /// <summary>
    /// Creates a generic advice payload using the supplied title and action text.
    /// </summary>
    public static DiagnosisAdvice Generic(string groupKey, string title, string message, string primaryAction, int priority = 5, string urgency = "Needs review")
    {
        return new DiagnosisAdvice(
            groupKey,
            priority,
            urgency,
            title,
            NormalizeText(primaryAction),
            message);
    }

    /// <summary>
    /// Creates advice for outdated-mod diagnoses after a game update.
    /// </summary>
    public static DiagnosisAdvice OutdatedMod()
    {
        return new DiagnosisAdvice(
            "outdated_mods",
            4,
            "Most likely fix",
            "One or more mods are outdated after a game update",
            "Remove these mods for now, or update them if newer versions are available.",
            "These mods are trying to use game code that changed in a recent update.");
    }

    /// <summary>
    /// Creates advice for a mod built for the wrong runtime variant.
    /// </summary>
    public static DiagnosisAdvice WrongRuntimeBuild()
    {
        return new DiagnosisAdvice(
            RuleIds.RuntimeMismatchMonoModOnIl2Cpp,
            2,
            "Most likely fix",
            "You have the wrong version of a mod installed",
            "Look for a version of this mod that says Il2Cpp. If you cannot find one, remove the mod.",
            "The installed copy was built for a different game setup.");
    }

    private static string NormalizeText(string value)
        => value.Replace("`", string.Empty, StringComparison.Ordinal).Trim();
}
