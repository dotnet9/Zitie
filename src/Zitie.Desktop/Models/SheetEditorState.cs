using Zitie.Core.Models;

namespace Zitie.Desktop.Models;

/// <summary>
/// 编辑器草稿的领域状态。它不依赖 Avalonia，便于文档恢复和单元测试。
/// </summary>
public sealed record SheetEditorState
{
    public const string DefaultInputText = "床前明月光，疑是地上霜。举头望明月，低头思故乡。";

    public string InputText { get; init; } = string.Empty;
    public PracticeLayoutKind PracticeLayout { get; init; }
    public bool BlankContentLayout { get; init; }
    public bool FillContentAreaWithBlankCells { get; init; }
    public string Title { get; init; } = string.Empty;
    public GridKind Grid { get; init; }
    public PracticeMode Mode { get; init; }
    public int RepeatsPerChar { get; init; } = 5;
    public int CellsPerLine { get; init; }
    public int LayoutColumns { get; init; }
    public int LayoutRows { get; init; }
    public int BlankCellLineCount { get; init; }
    public TraceIntensity TraceIntensity { get; init; } = TraceIntensity.Medium;
    public string GridColor { get; init; } = "#B04A3F";
    public string TextColor { get; init; } = "#1A1A1A";
    public SheetHeaderPreset HeaderPreset { get; init; } = SheetHeaderPreset.TitleAndFields;
    public string HeaderTextTemplate { get; init; } = "姓名_班级---年_月_日";
    public int TraceSlotCount { get; init; } = 2;
    public double GridSizeMm { get; init; } = 14;
    public double GridGapMm { get; init; } = 2;
    public double GroupGapMm { get; init; } = 2;
    public bool HollowGlyph { get; init; }
    public bool GroupByWord { get; init; }
    public bool ShowPinyin { get; init; }
    public bool PinyinOnly { get; init; }
    public SheetOrientation Orientation { get; init; }
    public bool ShowPoemHeader { get; init; }
    public bool FrameBorder { get; init; }
    public SheetBackground Background { get; init; }
    public string? BackgroundColor { get; init; }
    public string? BackgroundLineColor { get; init; }
    public double? BackgroundLineSpacingMm { get; init; }
    public string? BackgroundArtwork { get; init; }
    public string? BackgroundArtworkSvg { get; init; }
    public string? Author { get; init; }
    public string? Dynasty { get; init; }
    public string? FontFamilyName { get; init; }
    public string? TraceColor { get; init; }
    public PageSettings Page { get; init; } = PageSettings.A4;

    public static SheetEditorState CreateDefault(string? fontFamilyName = null)
    {
        return new SheetEditorState
        {
            InputText = DefaultInputText,
            Grid = GridKind.Mi,
            FontFamilyName = fontFamilyName
        };
    }

    public static SheetEditorState FromSpec(CharacterSheetSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        return new SheetEditorState
        {
            InputText = spec.BlankContentLayout ? string.Empty : spec.Text,
            PracticeLayout = spec.PracticeLayout,
            BlankContentLayout = spec.BlankContentLayout,
            FillContentAreaWithBlankCells = spec.FillContentAreaWithBlankCells,
            Title = spec.Title ?? string.Empty,
            Grid = spec.Grid,
            Mode = spec.Mode,
            RepeatsPerChar = spec.RepeatsPerChar,
            CellsPerLine = spec.CellsPerLine,
            LayoutColumns = spec.LayoutColumns,
            LayoutRows = spec.LayoutRows,
            BlankCellLineCount = spec.BlankCellLineCount,
            TraceIntensity = spec.TraceIntensity,
            GridColor = string.IsNullOrWhiteSpace(spec.GridColor) ? "#B04A3F" : spec.GridColor!,
            TextColor = string.IsNullOrWhiteSpace(spec.TextColor) ? "#1A1A1A" : spec.TextColor!,
            HeaderPreset = spec.HeaderPreset,
            HeaderTextTemplate = spec.HeaderTextTemplate ?? "姓名_班级---年_月_日",
            TraceSlotCount = spec.TraceSlotCount,
            GridSizeMm = spec.GridSizeMm,
            GridGapMm = spec.GridGapMm,
            GroupGapMm = spec.GroupGapMm,
            HollowGlyph = spec.HollowGlyph,
            GroupByWord = spec.GroupByWord,
            ShowPinyin = spec.ShowPinyin,
            PinyinOnly = spec.PinyinOnly,
            Orientation = spec.Orientation,
            ShowPoemHeader = spec.ShowPoemHeader,
            FrameBorder = spec.FrameBorder,
            Background = spec.Background,
            BackgroundColor = spec.BackgroundColor,
            BackgroundLineColor = spec.BackgroundLineColor,
            BackgroundLineSpacingMm = spec.BackgroundLineSpacingMm,
            BackgroundArtwork = spec.BackgroundArtwork,
            BackgroundArtworkSvg = spec.BackgroundArtworkSvg,
            Author = spec.Author,
            Dynasty = spec.Dynasty,
            FontFamilyName = spec.FontFamilyName,
            TraceColor = spec.TraceColor,
            Page = spec.Page
        };
    }
}
