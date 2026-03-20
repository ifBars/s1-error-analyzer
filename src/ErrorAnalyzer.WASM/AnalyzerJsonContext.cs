using System.Text.Json.Serialization;
using ErrorAnalyzer.Core;

namespace ErrorAnalyzer.WASM;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(LogAnalysisResultDto))]
[JsonSerializable(typeof(DiagnosisDto[]))]
internal partial class AnalyzerJsonContext : JsonSerializerContext
{
}
