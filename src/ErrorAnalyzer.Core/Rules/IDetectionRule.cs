namespace ErrorAnalyzer.Core;

internal interface IDetectionRule
{
    IEnumerable<Diagnosis> Analyze(LogDocument document);
}
