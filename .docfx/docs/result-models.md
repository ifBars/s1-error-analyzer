# Result models

The core library exposes both domain models and DTO models so callers can choose between typed enums and serialization-friendly string fields.

## Domain models

- <xref:ErrorAnalyzer.Core.LogAnalysisResult> contains the detected runtime and returned diagnoses.
- <xref:ErrorAnalyzer.Core.Diagnosis> describes one finding, including its rule id, evidence, severity, confidence, and advice payload.
- <xref:ErrorAnalyzer.Core.AnalysisProgress> reports progress phases while analysis is running.

## DTO models

- <xref:ErrorAnalyzer.Core.LogAnalysisResultDto> mirrors the result shape with string values for runtime, severity, and confidence.
- <xref:ErrorAnalyzer.Core.DiagnosisDto> is the serialized form of a diagnosis for browser, API, or bot consumers.

## Advice payloads

Each diagnosis includes <xref:ErrorAnalyzer.Core.Presentation.DiagnosisAdvice>, which carries presentation-oriented guidance such as urgency, summary text, and a primary action. Use <xref:ErrorAnalyzer.Core.Presentation.DiagnosisAdviceFactory> when you need to create consistent advice content from custom integrations.
