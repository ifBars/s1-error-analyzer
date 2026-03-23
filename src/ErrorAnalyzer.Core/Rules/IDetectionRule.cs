namespace ErrorAnalyzer.Core.Rules;

internal interface IDetectionRule
{
    IEnumerable<Diagnosis> Analyze(LogDocument document);
}
