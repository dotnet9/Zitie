using Zitie.Core.Layout;
using Zitie.Core.Models;

namespace Zitie.Desktop.Services;

/// <summary>根据模块 YAML 默认值生成模板画廊所需的首屏预览。</summary>
public static class ModulePreviewFactory
{
    public static ModulePreview Create(ModuleDefinition module)
    {
        var defaults = module.Defaults;

        var practiceLayout = PracticeLayoutKindParser.Parse(defaults.PracticeLayout);
        var grid = ParseGrid(defaults.Grid);
        var mode = ParseMode(defaults.Mode);
        var title = Normalize(defaults.Title);
        var vertical = defaults.Vertical == true;
        var blankContentLayout = defaults.BlankContentLayout == true;
        var requestedPoemHeader = defaults.ShowPoemHeader == true;
        var headerPreset = ParseHeaderPreset(
            defaults.HeaderPreset,
            title,
            requestedPoemHeader);
        var showPoemHeader = headerPreset == SheetHeaderPreset.Poem ||
                             (headerPreset != SheetHeaderPreset.None && requestedPoemHeader);
        var showHeaderFields = headerPreset is SheetHeaderPreset.Fields
            or SheetHeaderPreset.TitleAndFields
            or SheetHeaderPreset.Custom;
        var text = blankContentLayout ? string.Empty : Normalize(defaults.Text) ?? FallbackText(module.Id, grid);
        var repeats = Math.Clamp(defaults.Repeats ?? (mode == PracticeMode.Trace ? 5 : 3), 1, 8);
        var traceCount = Math.Clamp(defaults.TraceCount ?? 2, 0, repeats - 1);
        var page = ResolvePageSettings(defaults);

        var spec = new CharacterSheetSpec
        {
            Text = text,
            PracticeLayout = practiceLayout,
            BlankContentLayout = blankContentLayout,
            FillContentAreaWithBlankCells = defaults.FillContentAreaWithBlankCells == true,
            Grid = grid,
            Mode = mode,
            CellsPerLine = vertical
                ? 0
                : Math.Clamp(defaults.CellsPerLine ?? 12, 1, 64),
            LayoutColumns = Math.Clamp(defaults.LayoutColumns ?? 0, 0, 64),
            LayoutRows = Math.Clamp(defaults.LayoutRows ?? 0, 0, 128),
            BlankCellLineCount = Math.Clamp(defaults.BlankCellLineCount ?? 0, 0, 10),
            RepeatsPerChar = repeats,
            TraceSlotCount = traceCount,
            GridSizeMm = Math.Max(8, defaults.GridSize ?? 14),
            GridGapMm = Math.Max(0, defaults.GridGap ?? 2),
            GroupGapMm = Math.Clamp(defaults.GroupGap ?? 2, 1, 10),
            Title = headerPreset is SheetHeaderPreset.TitleAndFields
                or SheetHeaderPreset.Poem
                or SheetHeaderPreset.Custom
                ? title
                : null,
            HeaderPreset = headerPreset,
            HeaderTextTemplate = Normalize(defaults.HeaderText),
            ShowHeaderFields = showHeaderFields,
            Orientation = vertical ? SheetOrientation.Vertical : SheetOrientation.Horizontal,
            ShowPoemHeader = showPoemHeader,
            FrameBorder = defaults.FrameBorder == true,
            Background = ParseBackground(defaults.Background),
            BackgroundColor = Normalize(defaults.BackgroundColor),
            BackgroundLineColor = Normalize(defaults.BackgroundLineColor),
            BackgroundLineSpacingMm = defaults.BackgroundLineSpacing,
            BackgroundArtwork = Normalize(defaults.BackgroundArtwork),
            BackgroundArtworkSvg = module.ReadTextAsset(defaults.BackgroundArtwork),
            Author = Normalize(defaults.Author),
            Dynasty = Normalize(defaults.Dynasty),
            GroupByWord = defaults.GroupByWord == true,
            ShowPinyin = defaults.ShowPinyin == true,
            PinyinOnly = defaults.PinyinOnly == true,
            HollowGlyph = defaults.HollowGlyph == true,
            TraceIntensity = ParseTraceIntensity(defaults.TraceIntensity),
            TraceColor = Normalize(defaults.TraceColor),
            GridColor = Normalize(defaults.GridColor),
            TextColor = Normalize(defaults.TextColor),
            FontFamilyName = Normalize(defaults.FontFamily),
            Page = page
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
        if (module.Categories.Count > 0) return module.Categories[0];
        if (grid == GridKind.English) return "英文";
        if (grid == GridKind.Pinyin) return "拼音";
        if (vertical) return "书法";
        return "汉字";
    }

    private static GridKind ParseGrid(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "none" => GridKind.None,
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
            "ricepaper" => SheetBackground.RicePaper,
            _ => SheetBackground.Plain
        };
    }

    private static PageSettings ResolvePageSettings(ModuleDefaults defaults)
    {
        var page = defaults.PageSize?.ToLowerInvariant() switch
        {
            "a4landscape" => new PageSettings { WidthMm = 297, HeightMm = 210 },
            "a3portrait" => new PageSettings { WidthMm = 297, HeightMm = 420 },
            "letter" => new PageSettings { WidthMm = 216, HeightMm = 279 },
            _ => PageSettings.A4
        };

        if (defaults.PageMargin is { } margin)
        {
            var value = ClampMargin(margin);
            page = page with
            {
                MarginTopMm = value,
                MarginBottomMm = value,
                MarginLeftMm = value,
                MarginRightMm = value
            };
        }

        return page with
        {
            MarginTopMm = ClampMargin(defaults.PageMarginTop ?? page.MarginTopMm),
            MarginBottomMm = ClampMargin(defaults.PageMarginBottom ?? page.MarginBottomMm),
            MarginLeftMm = ClampMargin(defaults.PageMarginLeft ?? page.MarginLeftMm),
            MarginRightMm = ClampMargin(defaults.PageMarginRight ?? page.MarginRightMm)
        };
    }

    private static double ClampMargin(double value)
    {
        return Math.Clamp(value, 0, 80);
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

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public sealed record ModulePreview(
    CharacterSheetSpec Spec,
    IReadOnlyList<SheetPage> Pages,
    string Category);
