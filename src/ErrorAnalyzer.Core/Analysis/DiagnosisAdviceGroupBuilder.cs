using ErrorAnalyzer.Core.Models;

namespace ErrorAnalyzer.Core.Analysis;

internal static class DiagnosisAdviceGroupBuilder
{
    public static IReadOnlyList<DiagnosisAdviceGroupDto> Build(IReadOnlyList<Diagnosis> diagnoses)
    {
        return diagnoses
            .GroupBy(diagnosis => diagnosis.Advice.GroupKey, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var primaryDiagnosis = group
                    .OrderBy(diagnosis => diagnosis.Advice.Priority)
                    .ThenBy(diagnosis => diagnosis.LineNumber)
                    .First();
                var affectedMods = group
                    .Select(diagnosis => ModNameNormalizer.Normalize(diagnosis.ModName))
                    .Where(modName => !string.IsNullOrWhiteSpace(modName))
                    .Select(modName => modName!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(modName => modName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                return new DiagnosisAdviceGroupDto(
                    primaryDiagnosis.Advice.GroupKey,
                    primaryDiagnosis.Advice.Priority,
                    primaryDiagnosis.Advice.Urgency,
                    primaryDiagnosis.Advice.Title,
                    primaryDiagnosis.Advice.PrimaryAction,
                    primaryDiagnosis.Advice.Explanation,
                    affectedMods,
                    group.Count(),
                    group.Sum(diagnosis => diagnosis.OccurrenceCount));
            })
            .OrderBy(group => group.Priority)
            .ThenBy(group => group.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
