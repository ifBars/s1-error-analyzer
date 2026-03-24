using System.Text.Json.Serialization;
using ErrorAnalyzer.Core.Models;
using ErrorAnalyzer.Core.Presentation;

namespace ErrorAnalyzer.WASM;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(LogAnalysisResultDto))]
[JsonSerializable(typeof(DiagnosisDto[]))]
[JsonSerializable(typeof(DiagnosisAdviceGroupDto[]))]
[JsonSerializable(typeof(DiagnosisAdvice))]
internal partial class AnalyzerJsonContext : JsonSerializerContext
{
}
