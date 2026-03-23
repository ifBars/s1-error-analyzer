# Analysis pipeline

`ErrorAnalyzer.Core` processes a log in a few predictable stages so each host can present consistent findings.

## 1. Parse the document

<xref:ErrorAnalyzer.Core.LogAnalyzer> normalizes the input text into a `LogDocument`, preserving line numbers. During this stage the library also detects the runtime flavor by scanning for Mono and IL2CPP markers in the log.

## 2. Run detection rules

The analyzer executes its built-in detection rules in sequence. Each rule inspects nearby lines, stack traces, assembly markers, or runtime signals to emit one or more <xref:ErrorAnalyzer.Core.Diagnosis> instances.

The default rule set covers:

- duplicate runtime installs
- mods in the wrong folder
- missing patch targets
- missing methods
- missing dependencies
- runtime mismatches
- outdated type references
- IL2CPP field accessor patch breakage

## 3. Aggregate duplicate findings

Before returning, the analyzer groups repeated matches with the same diagnosis fingerprint. This keeps the final result focused while preserving an `OccurrenceCount` on the diagnosis model.

## 4. Map to DTOs when needed

Hosts that need a transport-safe shape can convert the result with <xref:ErrorAnalyzer.Core.Analysis.LogAnalysisResultMapper> or by calling the DTO-returning methods directly on <xref:ErrorAnalyzer.Core.LogAnalyzer>.
