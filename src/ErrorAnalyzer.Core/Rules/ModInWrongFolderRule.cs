using System.Text.RegularExpressions;

namespace ErrorAnalyzer.Core;

internal sealed class ModInWrongFolderRule : IDetectionRule
{
    private static readonly Regex MelonNameRegex = new(@"Failed to load Melon '(?<mod>[^']+)'", RegexOptions.Compiled);

    public IEnumerable<Diagnosis> Analyze(LogDocument document)
    {
        foreach (var line in document.Lines)
        {
            if (!line.Text.Contains("Failed to load Melon", StringComparison.Ordinal) ||
                !line.Text.Contains("The given Melon is a Mod and cannot be loaded as a Plugin", StringComparison.Ordinal))
            {
                continue;
            }

            var modMatch = MelonNameRegex.Match(line.Text);
            var modName = modMatch.Success ? modMatch.Groups["mod"].Value : null;

            yield return new Diagnosis(
                RuleIds.ModInWrongFolder,
                "This mod is in the wrong folder",
                "This file was installed into the `Plugins` folder even though MelonLoader identifies it as a mod.",
                "Move this file from `Plugins` into the `Mods` folder, then launch the game again.",
                modName,
                line.Text.Trim(),
                line.Number,
                DiagnosisSeverity.Error,
                DiagnosisConfidence.High,
                new DiagnosisAdvice(
                    RuleIds.ModInWrongFolder,
                    1,
                    "Quick fix",
                    "A mod was installed into the wrong folder",
                    "Move this file from Plugins into Mods, then try again.",
                    "MelonLoader recognized this file as a mod, not a plugin."));
        }
    }
}
