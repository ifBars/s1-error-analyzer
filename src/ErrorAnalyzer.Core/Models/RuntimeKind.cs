namespace ErrorAnalyzer.Core.Models;

/// <summary>
/// Identifies which runtime flavor the analyzed log appears to be using.
/// </summary>
public enum RuntimeKind
{
    /// <summary>
    /// The analyzer could not determine the runtime from the supplied log.
    /// </summary>
    Unknown,

    /// <summary>
    /// The log appears to come from the Mono runtime.
    /// </summary>
    Mono,

    /// <summary>
    /// The log appears to come from the IL2CPP runtime.
    /// </summary>
    Il2Cpp,
}
