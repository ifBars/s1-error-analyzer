#pragma warning disable CS1591

namespace ErrorAnalyzer.Core.Models;

/// <summary>
/// Reports incremental progress while analysis is running.
/// </summary>
public sealed class AnalysisProgress
{
    public AnalysisProgress()
    {
        Phase = string.Empty;
    }

    public AnalysisProgress(string phase, double progress)
    {
        Phase = phase;
        Progress = progress;
    }

    public string Phase { get; set; }

    public double Progress { get; set; }
}

#pragma warning restore CS1591
