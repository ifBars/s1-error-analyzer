namespace ErrorAnalyzer.Core;

public static class DiagnosisAdviceFactory
{
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

    private static string NormalizeText(string value)
        => value.Replace("`", string.Empty, StringComparison.Ordinal).Trim();
}
