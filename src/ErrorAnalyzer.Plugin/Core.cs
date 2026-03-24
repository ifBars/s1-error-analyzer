using ErrorAnalyzer.Core;
using ErrorAnalyzer.Core.Models;
using ErrorAnalyzer.Core.Presentation;
using MelonLoader;
using MelonLoader.Utils;

[assembly: MelonInfo(typeof(ErrorAnalyzer.Plugin.ErrorAnalyzerMelon), "ErrorAnalyzer", ErrorAnalyzerBuildInfo.Version, "OpenCode")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ErrorAnalyzer.Plugin;

public sealed class ErrorAnalyzerMelon : MelonMod
{
    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(5);

    private readonly LogAnalyzer _analyzer = new();
    private readonly HashSet<string> _reportedFingerprints = new(StringComparer.Ordinal);
    private readonly List<UserAdviceCard> _adviceCards = new();

    private DateTime _lastScanAtUtc;
    private DateTime _lastLogWriteAtUtc;
    private bool _showOverlay;

    public override void OnInitializeMelon()
    {
        LoggerInstance.Msg("ErrorAnalyzer initialized.");
        ScanLatestLog(force: true);
    }

    public override void OnUpdate()
    {
        if (DateTime.UtcNow - _lastScanAtUtc < ScanInterval)
        {
            return;
        }

        ScanLatestLog(force: false);
    }

    public override void OnGUI()
    {
        if (!_showOverlay || _adviceCards.Count == 0)
        {
            return;
        }

        var screenWidth = GuiBridge.ScreenWidth;
        var screenHeight = GuiBridge.ScreenHeight;
        var width = Math.Min(760f, screenWidth - 40f);
        var height = Math.Min(460f, screenHeight - 40f);
        var x = (screenWidth - width) / 2f;
        var y = (screenHeight - height) / 2f;

        GuiBridge.Box(0f, 0f, screenWidth, screenHeight, string.Empty);
        GuiBridge.Box(x, y, width, height, "Mod problems found");

        var textY = y + 38f;
        GuiBridge.Label(
            x + 20f, textY, width - 40f, 54f,
            "Some installed mods are broken for this version of the game. Follow the steps below so the game can load normally.");

        var cardY = textY + 60f;
        for (var index = 0; index < _adviceCards.Count && index < 3; index++)
        {
            var card = _adviceCards[index];
            var boxHeight = 94f;
            GuiBridge.Box(x + 18f, cardY, width - 36f, boxHeight, string.Empty);
            GuiBridge.Label(x + 30f, cardY + 10f, width - 60f, 20f, card.Title);
            GuiBridge.Label(x + 30f, cardY + 32f, width - 60f, 36f, card.PrimaryAction);
            GuiBridge.Label(x + 30f, cardY + 66f, width - 60f, 20f, $"Mods: {string.Join(", ", card.ModNames)}");
            cardY += boxHeight + 10f;
        }

        if (GuiBridge.Button(x + 20f, y + height - 54f, 160f, 34f, "Close"))
        {
            _showOverlay = false;
        }

        GuiBridge.Label(
            x + 200f, y + height - 52f, width - 220f, 34f,
            "Tip: update the listed mods, or remove them if no updated version exists.");
    }

    private void ScanLatestLog(bool force)
    {
        _lastScanAtUtc = DateTime.UtcNow;
        var latestLog = FindLatestLogPath();
        if (latestLog is null)
        {
            return;
        }

        var lastWriteAtUtc = File.GetLastWriteTimeUtc(latestLog);
        if (!force && lastWriteAtUtc <= _lastLogWriteAtUtc)
        {
            return;
        }

        _lastLogWriteAtUtc = lastWriteAtUtc;
        var result = _analyzer.AnalyzeFile(latestLog);
        var hasNewDiagnosis = false;

        foreach (var diagnosis in result.Diagnoses)
        {
            if (!_reportedFingerprints.Add(diagnosis.Fingerprint))
            {
                continue;
            }

            hasNewDiagnosis = true;
            LoggerInstance.Warning(FormatDiagnosis(diagnosis));
        }

        if (hasNewDiagnosis)
        {
            _adviceCards.Clear();
            _adviceCards.AddRange(BuildAdviceCards(result.Diagnoses));
            _showOverlay = _adviceCards.Count > 0;
        }
    }

    private static string? FindLatestLogPath()
    {
        foreach (var directory in CandidateLogDirectories())
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            var latest = Directory
                .EnumerateFiles(directory, "*.log", SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .OrderByDescending(info => info.LastWriteTimeUtc)
                .FirstOrDefault();

            if (latest is not null)
            {
                return latest.FullName;
            }
        }

        return null;
    }

    private static IEnumerable<string> CandidateLogDirectories()
    {
        yield return MelonEnvironment.MelonLoaderLogsDirectory;
        yield return Path.Combine(MelonEnvironment.GameRootDirectory, "ErrorLogs");
        yield return MelonEnvironment.GameRootDirectory;
    }

    private static string FormatDiagnosis(Diagnosis diagnosis)
    {
        var owner = string.IsNullOrWhiteSpace(diagnosis.ModName) ? "unknown mod" : diagnosis.ModName;
        return $"{owner}: {diagnosis.Advice.PrimaryAction}";
    }

    private static IReadOnlyList<UserAdviceCard> BuildAdviceCards(IReadOnlyList<Diagnosis> diagnoses)
    {
        var cards = new List<UserAdviceCard>();

        foreach (var group in diagnoses.GroupBy(diagnosis => diagnosis.Advice.GroupKey, StringComparer.OrdinalIgnoreCase))
        {
            var primaryDiagnosis = group
                .OrderBy(diagnosis => diagnosis.Advice.Priority)
                .ThenBy(diagnosis => diagnosis.LineNumber)
                .First();

            var card = CreateAdviceCard(primaryDiagnosis);
            foreach (var diagnosis in group)
            {
                card.AddMod(diagnosis.ModName);
            }

            cards.Add(card);
        }

        return cards
            .OrderBy(card => card.Priority)
            .ToArray();
    }

    private static UserAdviceCard CreateAdviceCard(Diagnosis diagnosis)
    {
        return new UserAdviceCard(diagnosis.Advice, diagnosis.ModName);
    }
}

internal static class GuiBridge
{
    private static readonly Type? ScreenType = Type.GetType("UnityEngine.Screen, UnityEngine.CoreModule");
    private static readonly Type? RectType = Type.GetType("UnityEngine.Rect, UnityEngine.CoreModule");
    private static readonly Type? GuiType = Type.GetType("UnityEngine.GUI, UnityEngine.IMGUIModule")
        ?? Type.GetType("UnityEngine.GUI, UnityEngine");

    public static float ScreenWidth => ReadScreenValue("width");

    public static float ScreenHeight => ReadScreenValue("height");

    public static void Box(float x, float y, float width, float height, string text)
        => InvokeGui("Box", x, y, width, height, text);

    public static void Label(float x, float y, float width, float height, string text)
        => InvokeGui("Label", x, y, width, height, text);

    public static bool Button(float x, float y, float width, float height, string text)
    {
        var result = InvokeGui("Button", x, y, width, height, text);
        return result is bool clicked && clicked;
    }

    private static object? InvokeGui(string methodName, float x, float y, float width, float height, string text)
    {
        if (GuiType is null || RectType is null)
        {
            return null;
        }

        var method = GuiType.GetMethod(methodName, new[] { RectType, typeof(string) });
        if (method is null)
        {
            return null;
        }

        var rect = Activator.CreateInstance(RectType, x, y, width, height);
        return method.Invoke(null, new[] { rect, text });
    }

    private static float ReadScreenValue(string propertyName)
    {
        if (ScreenType is null)
        {
            return 1280f;
        }

        var property = ScreenType.GetProperty(propertyName);
        var value = property?.GetValue(null);
        return value switch
        {
            int intValue => intValue,
            float floatValue => floatValue,
            _ => 1280f,
        };
    }
}

internal sealed class UserAdviceCard
{
    public UserAdviceCard(DiagnosisAdvice advice, string? modName)
    {
        Key = advice.GroupKey;
        Priority = advice.Priority;
        Title = advice.Title;
        PrimaryAction = advice.PrimaryAction;
        ModNames = new List<string>();
        AddMod(modName);
    }

    public string Key { get; }

    public int Priority { get; }

    public string Title { get; }

    public string PrimaryAction { get; }

    public List<string> ModNames { get; }

    public void AddMod(string? modName)
    {
        var name = string.IsNullOrWhiteSpace(modName) ? "Unknown mod" : modName;
        if (!ModNames.Any(existing => string.Equals(existing, name, StringComparison.OrdinalIgnoreCase)))
        {
            ModNames.Add(name);
            ModNames.Sort(StringComparer.OrdinalIgnoreCase);
        }
    }
}
