using ErrorAnalyzer.Core.Models;
using ErrorAnalyzer.Core.Parsing;

namespace ErrorAnalyzer.Core.Rules;

internal interface IDetectionRule
{
    IEnumerable<Diagnosis> Analyze(LogDocument document);
}
