namespace ErrorAnalyzer.Core.Models;

/// <summary>
/// Indicates how confident the analyzer is in a diagnosis.
/// </summary>
public enum DiagnosisConfidence
{
    /// <summary>
    /// Weak signal that may need manual verification.
    /// </summary>
    Low,

    /// <summary>
    /// Strong enough signal for a likely match.
    /// </summary>
    Medium,

    /// <summary>
    /// Highly specific match with little ambiguity.
    /// </summary>
    High,
}
