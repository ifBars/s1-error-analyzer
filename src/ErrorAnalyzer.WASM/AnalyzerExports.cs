using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading.Tasks;
using ErrorAnalyzer.Core;

namespace ErrorAnalyzer.WASM;

[SupportedOSPlatform("browser")]
public partial class AnalyzerExports
{
    private static readonly LogAnalyzer Analyzer = new();

    [JSImport("globalThis.__scheduleOneAnalyzerReportProgress")]
    internal static partial void ReportProgress(string phase, double progress);

    [JSImport("globalThis.__scheduleOneAnalyzerYieldToUi")]
    internal static partial Task YieldToUi();

    [JSExport]
    public static async Task<string> AnalyzeLogAsync(string text, string sourceName)
    {
        var result = await Analyzer.AnalyzeTextAsDtoAsync(text, sourceName, static progress => TryReportProgressAsync(progress));
        return JsonSerializer.Serialize(result, AnalyzerJsonContext.Default.LogAnalysisResultDto);
    }

    [JSExport]
    public static string GetVersion()
    {
        return $"{ErrorAnalyzerBuildInfo.Version}-wasm";
    }

    private static void TryReportProgress(AnalysisProgress progress)
    {
        try
        {
            ReportProgress(progress.Phase, progress.Progress);
        }
        catch
        {
        }
    }

    private static async Task TryReportProgressAsync(AnalysisProgress progress)
    {
        TryReportProgress(progress);

        try
        {
            await YieldToUi();
        }
        catch
        {
        }
    }
}
