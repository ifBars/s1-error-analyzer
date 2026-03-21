namespace ErrorAnalyzer.Core;

public static class RuleIds
{
    public const string DualRuntimeInstall = "dual_runtime_install";
    public const string MissingPatchTarget = "missing_patch_target";
    public const string MissingMethod = "missing_method";
    public const string MissingDependency = "missing_dependency";
    public const string ModInWrongFolder = "mod_in_wrong_folder";
    public const string RuntimeMismatchMonoModOnIl2Cpp = "runtime_mismatch_mono_mod_on_il2cpp";
    public const string OutdatedTypeReference = "outdated_type_reference";
    public const string FieldAccessorPatchBreak = "field_accessor_patch_break";
}
