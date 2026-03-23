# Getting started

The main entry point is <xref:ErrorAnalyzer.Core.LogAnalyzer>. It can analyze raw log text, read a log file from disk, or return DTOs that are easier to serialize across app boundaries.

## Analyze raw text

```csharp
using ErrorAnalyzer.Core;

var analyzer = new LogAnalyzer();
var result = analyzer.AnalyzeText(logText);

foreach (var diagnosis in result.Diagnoses)
{
    Console.WriteLine($"{diagnosis.RuleId}: {diagnosis.Title}");
}
```

## Report progress

`AnalyzeText` accepts an optional callback that receives <xref:ErrorAnalyzer.Core.AnalysisProgress> updates for parsing, runtime detection, rule execution, aggregation, and finalization.

```csharp
var analyzer = new LogAnalyzer();

var result = analyzer.AnalyzeText(
    logText,
    progress => Console.WriteLine($"{progress.Phase} ({progress.Progress:P0})"));
```

## Use DTO output

If your caller needs string-backed enums and a serialization-friendly shape, use <xref:ErrorAnalyzer.Core.LogAnalyzer.AnalyzeTextAsDto(System.String)> or <xref:ErrorAnalyzer.Core.LogAnalyzer.AnalyzeFileAsDto(System.String)>.

```csharp
var analyzer = new LogAnalyzer();
var dto = analyzer.AnalyzeTextAsDto(logText);

Console.WriteLine(dto.Runtime);
Console.WriteLine(dto.Diagnoses.Count);
```
