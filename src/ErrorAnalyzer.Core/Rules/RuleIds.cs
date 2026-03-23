namespace ErrorAnalyzer.Core;

/// <summary>
/// Stable identifiers emitted by the built-in detection rules.
/// </summary>
public static class RuleIds
{
    /// <summary>
    /// Indicates both Mono and IL2CPP variants of the same mod were loaded.
    /// </summary>
    public const string DualRuntimeInstall = "dual_runtime_install";

    /// <summary>
    /// Indicates a Harmony patch target could not be found.
    /// </summary>
    public const string MissingPatchTarget = "missing_patch_target";

    /// <summary>
    /// Indicates a required method could not be found in the target runtime.
    /// </summary>
    public const string MissingMethod = "missing_method";

    /// <summary>
    /// Indicates a required dependency assembly could not be found.
    /// </summary>
    public const string MissingDependency = "missing_dependency";

    /// <summary>
    /// Indicates a mod appears to be installed in the wrong folder.
    /// </summary>
    public const string ModInWrongFolder = "mod_in_wrong_folder";

    /// <summary>
    /// Indicates a Mono mod appears to be running against an IL2CPP game build.
    /// </summary>
    public const string RuntimeMismatchMonoModOnIl2Cpp = "runtime_mismatch_mono_mod_on_il2cpp";

    /// <summary>
    /// Indicates a mod references a type that is no longer available in the game build.
    /// </summary>
    public const string OutdatedTypeReference = "outdated_type_reference";

    /// <summary>
    /// Indicates an IL2CPP field accessor patch pattern appears to have broken.
    /// </summary>
    public const string FieldAccessorPatchBreak = "field_accessor_patch_break";
}
