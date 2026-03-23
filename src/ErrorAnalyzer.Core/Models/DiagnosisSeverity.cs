namespace ErrorAnalyzer.Core.Models;

/// <summary>
/// Describes how severe a diagnosis is for the caller.
/// </summary>
public enum DiagnosisSeverity
{
    /// <summary>
    /// Informational finding with no immediate failure implied.
    /// </summary>
    Info,

    /// <summary>
    /// Warning-level finding that may explain instability or incompatibility.
    /// </summary>
    Warning,

    /// <summary>
    /// Error-level finding that is likely to break the mod or runtime.
    /// </summary>
    Error,
}
