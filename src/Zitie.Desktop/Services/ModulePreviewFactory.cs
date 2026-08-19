using System.Text.Json;
using Zitie.Core.Layout;
using Zitie.Core.Models;

namespace Zitie.Desktop.Services;

/// <summary>根据模块 JSON 默认值生成模板画廊所需的首屏预览。</summary>
public static class ModulePreviewFactory
{
    private static readonly PageSettings PreviewPage = PageSettings.A4;

    public static ModulePreview Create(ModuleDefinition module)
    {
        var defaults = module.Defaults is { ValueKind: JsonValueKind.Object } value
            ? value
            : default;

        var grid = ParseGrid(GetString(defaults, "grid"));
        var mode = ParseMode(GetString(defaults, "mode"));
        var title = GetString(defaults, "title");
        var vertical = GetBoolean(defaults, "vertical");
        var requestedPoemHeader = GetBoolean(defaults, "showPoemHeader");
        var headerPreset = ParseHeaderPreset(
            GetString(defaults, "headerPreset"),
            title,
            requestedPoemHeader);
        var showPoemHeader = headerPreset == SheetHeaderPreset.Poem ||
                             (headerPreset != SheetHeaderPreset.None && requestedPoemHeader);
        var showHeaderFields = headerPreset is SheetHeaderPreset.Fields
            or SheetHeaderPreset.TitleAndFields
            or SheetHeaderPreset.Custom;
        var text = GetString(defaults, "text") ?? FallbackText(module.Id, grid);
        var repeats = Math.Clamp(GetInt32(defaults, "repeats") ?? (mode == PracticeMode.Trace ? 5 : 3), 1, 8);
        var traceCount = Math.Clamp(GetInt32(defaults, "traceCount") ?? 2, 0, repeats - 1);

        var spec = new CharacterSheetSpec
        {
            Text = text,
            Grid = grid,
            Mode = mode,
            CharactersPerLine = vertical
                ? 0
                : Math.Clamp(GetInt32(defaults, "charactersPerLine") ?? 12, 1, 64),
            BlankLineCount = Math.Clamp(GetInt32(defaults, "blankLineCount") ?? 0, 0, 10),
            RepeatsPerChar = repeats,
            TraceSlotCount = traceCount,
            GridSizeMm = Math.Max(8, GetDouble(defaults, "gridSize") ?? 14),
            GridGapMm = Math.Max(0, GetDouble(defaults, "gridGap") ?? 2),
            Title = headerPreset is SheetHeaderPreset.TitleAndFields
                or SheetHeaderPreset.Poem
                or SheetHeaderPreset.Custom
                ? title
                : null,
            HeaderPreset = headerPreset,
            HeaderTextTemplate = GetString(defaults, "headerText"),
            ShowHeaderFields = showHeaderFields,
            Orientation = vertical ? SheetOrientation.Vertical : SheetOrientation.Horizontal,
            ShowPoemHeader = showPoemHeader,
            FrameBorder = GetBoolean(defaults, "frameBorder"),
            Background = ParseBackground(GetString(defaults, "background")),
            Author = GetString(defaults, "author"),
            Dynasty = GetString(defaults, "dynasty"),
            GroupByWord = GetBoolean(defaults, "groupByWord"),
            ShowPinyin = GetBoolean(defaults, "showPinyin"),
            PinyinOnly = GetBoolean(defaults, "pinyinOnly"),
            HollowGlyph = GetBoolean(defaults, "hollowGlyph"),
            TraceIntensity = ParseTraceIntensity(GetString(defaults, "traceIntensity")),
            TraceColor = GetString(defaults, "traceColor"),
            GridColor = GetString(defaults, "gridColor"),
            TextColor = GetString(defaults, "textColor"),
            FontFamilyName = GetString(defaults, "fontFamily"),
            Page = PreviewPage
        };

        return new ModulePreview(
            spec,
            LayoutEngine.Paginate(spec),
            ResolveCategory(module, grid, mode, vertical));
    }

    private static string ResolveCategory(
        ModuleDefinition module,
        GridKind grid,
        PracticeMode mode,
        bool vertical)
    {
        if (grid is GridKind.English or GridKind.Pinyin) return "拼音 / 英文";
        if (vertical || module.Id.Contains("poem", StringComparison.OrdinalIgnoreCase)) return "诗词排版";
        var hollowGlyph = module.Defaults is { ValueKind: JsonValueKind.Object } defaults &&
                          GetBoolean(defaults, "hollowGlyph");
        if (hollowGlyph || module.Id.Contains("hollow", StringComparison.OrdinalIgnoreCase))
            return "双钩临摹";
        return mode == PracticeMode.Trace ? "描红练习" : "基础临摹";
    }

    private static GridKind ParseGrid(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "tian" => GridKind.Tian,
            "huigong" => GridKind.HuiGong,
            "plain" => GridKind.Plain,
            "english" => GridKind.English,
            "nine" => GridKind.Nine,
            "pinyin" => GridKind.Pinyin,
            _ => GridKind.Mi
        };
    }

    private static PracticeMode ParseMode(string? value)
    {
        return string.Equals(value, "copy", StringComparison.OrdinalIgnoreCase)
            ? PracticeMode.Copy
            : PracticeMode.Trace;
    }

    private static SheetBackground ParseBackground(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "redgrid" => SheetBackground.RedGrid,
            "letter" => SheetBackground.Letter,
            _ => SheetBackground.Plain
        };
    }

    private static TraceIntensity ParseTraceIntensity(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "verydark" => TraceIntensity.VeryDark,
            "dark" => TraceIntensity.Dark,
            "mediumdark" => TraceIntensity.MediumDark,
            "light" => TraceIntensity.Light,
            "verylight" => TraceIntensity.VeryLight,
            "white" => TraceIntensity.White,
            "hollow" => TraceIntensity.Hollow,
            _ => TraceIntensity.Medium
        };
    }

    private static SheetHeaderPreset ParseHeaderPreset(
        string? value,
        string? title,
        bool showPoemHeader)
    {
        return value?.ToLowerInvariant() switch
        {
            "none" => SheetHeaderPreset.None,
            "fields" => SheetHeaderPreset.Fields,
            "titleandfields" => SheetHeaderPreset.TitleAndFields,
            "poem" => SheetHeaderPreset.Poem,
            "custom" => SheetHeaderPreset.Custom,
            _ when showPoemHeader => SheetHeaderPreset.Poem,
            _ when title is not null => SheetHeaderPreset.TitleAndFields,
            _ => SheetHeaderPreset.None
        };
    }

    private static string FallbackText(string moduleId, GridKind grid)
    {
        if (grid == GridKind.English) return "cat dog pig cow sheep";
        if (grid == GridKind.Pinyin) return "chūn tiān huā duǒ";
        if (moduleId.Contains("vertical", StringComparison.OrdinalIgnoreCase))
            return "床前明月光，疑是地上霜。举头望明月，低头思故乡。";
        if (moduleId.Contains("pinyin", StringComparison.OrdinalIgnoreCase))
            return "春天 花朵";
        return "春风化雨";
    }

    private static string? GetString(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(name, out var value) ||
            value.ValueKind != JsonValueKind.String)
            return null;

        var result = value.GetString();
        return string.IsNullOrWhiteSpace(result) ? null : result.Trim();
    }

    private static bool GetBoolean(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object &&
               element.TryGetProperty(name, out var value) &&
               value.ValueKind is JsonValueKind.True or JsonValueKind.False &&
               value.GetBoolean();
    }

    private static int? GetInt32(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object &&
               element.TryGetProperty(name, out var value) &&
               value.TryGetInt32(out var result)
            ? result
            : null;
    }

    private static double? GetDouble(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object &&
               element.TryGetProperty(name, out var value) &&
               value.TryGetDouble(out var result)
            ? result
            : null;
    }
}

public sealed record ModulePreview(
    CharacterSheetSpec Spec,
    IReadOnlyList<SheetPage> Pages,
    string Category);
