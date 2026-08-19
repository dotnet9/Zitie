using System.Text.Json;
using Avalonia.Media;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Zitie.Avalonia.Export;
using Zitie.Core.Layout;
using Zitie.Core.Models;
using Zitie.Desktop.Services;

namespace Zitie.Desktop.ViewModels;

/// <summary>
///     字帖编辑页：左侧配置驱动 <see cref="Rebuild" /> 重新排版，右侧预览消费同一份结果。
/// </summary>
public class SheetEditorViewModel : BindableBase, INavigationAware
{
    private readonly ModuleCatalog _catalog;
    private readonly TextCatalog _textCatalog;
    private readonly PinyinCatalog _pinyinCatalog;
    private readonly FontCatalog _fontCatalog;
    private readonly IRegionNavigationJournal _journal;

    private string _inputText = "床前明月光，疑是地上霜。举头望明月，低头思故乡。";
    private string _title = string.Empty;
    private int _gridKindIndex;
    private int _practiceModeIndex;
    private int _repeatsIndex = 4;
    private int _charactersPerLineIndex;
    private int _blankLineCount;
    private int _traceIntensityIndex = 3;
    private int _gridColorIndex;
    private int _textColorIndex = 1;
    private int _headerPresetIndex = 2;
    private int _traceSlotCount = 2;
    private double _gridSizeMm = 14;
    private bool _hollowGlyph;
    private bool _groupByWord;
    private bool _showPinyin;
    private bool _pinyinOnly;
    private bool _isVertical;
    private bool _showPoemHeader;
    private bool _frameBorder;
    private int _backgroundIndex;
    private string? _backgroundColor;
    private string? _backgroundLineColor;
    private double? _backgroundLineSpacingMm;
    private string _author = string.Empty;
    private string _dynasty = string.Empty;
    private FontOption? _selectedSheetFont;
    private string _headerTextTemplate = "姓名_班级---年_月_日";
    private int _textEntryIndex = -1;
    private double _zoom = 1.0;
    private int _pageIndex;
    private string _traceColor = string.Empty;
    private CharacterSheetSpec _spec = new();
    private IReadOnlyList<SheetPage> _pages = Array.Empty<SheetPage>();
    private ModuleDefinition? _module;

    private static readonly string[] ColorChoiceValues =
    [
        "#B04A3F",
        "#1A1A1A",
        "#2F9E44"
    ];

    public SheetEditorViewModel(
        ModuleCatalog catalog,
        TextCatalog textCatalog,
        PinyinCatalog pinyinCatalog,
        FontCatalog fontCatalog,
        IRegionNavigationJournal journal)
    {
        _catalog = catalog;
        _textCatalog = textCatalog;
        _pinyinCatalog = pinyinCatalog;
        _fontCatalog = fontCatalog;
        _journal = journal;
        _selectedSheetFont = _fontCatalog.Find(_fontCatalog.DefaultFontFamily) ??
                             _fontCatalog.Fonts.FirstOrDefault();

        GoBackCommand = new DelegateCommand(() => _journal.GoBack());
        PreviousPageCommand = new DelegateCommand(
            () => PageIndex--,
            () => PageIndex > 0)
            .ObservesProperty(() => PageIndex);
        NextPageCommand = new DelegateCommand(
            () => PageIndex++,
            () => PageIndex < PageCount - 1)
            .ObservesProperty(() => PageIndex)
            .ObservesProperty(() => PageCount);
    }

    public int[] RepeatsChoices { get; } = Enumerable.Range(1, 8).ToArray();

    public int[] CharactersPerLineChoices { get; } = [12, 16];

    public int[] BlankLineChoices { get; } = Enumerable.Range(0, 11).ToArray();

    public string[] ColorChoices { get; } = ["红色", "黑色", "绿色"];

    public string[] TraceIntensityChoices { get; } =
    [
        "非常深",
        "深",
        "较深",
        "适中",
        "略浅",
        "非常浅",
        "白色",
        "空心"
    ];

    public string[] HeaderPresetChoices { get; } =
    [
        "无页头",
        "填写栏",
        "标题 + 填写栏",
        "诗词题头",
        "自定义"
    ];

    public IReadOnlyList<FontOption> SheetFonts => _fontCatalog.Fonts;

    /// <summary>文本库条目（下拉选择后填充 InputText）。</summary>
    public IReadOnlyList<TextEntry> TextEntries => _textCatalog.Entries;

    public int CharactersPerLineIndex
    {
        get => _charactersPerLineIndex;
        set
        {
            if (SetProperty(ref _charactersPerLineIndex,
                    Math.Clamp(value, 0, CharactersPerLineChoices.Length - 1))) Rebuild();
        }
    }

    public int BlankLineCount
    {
        get => _blankLineCount;
        set
        {
            if (SetProperty(ref _blankLineCount, Math.Clamp(value, 0, 10))) Rebuild();
        }
    }

    public int TraceIntensityIndex
    {
        get => _traceIntensityIndex;
        set
        {
            if (!SetProperty(ref _traceIntensityIndex, Math.Clamp(value, 0, TraceIntensityChoices.Length - 1)))
                return;

            _traceColor = string.Empty;
            RaisePropertyChanged(nameof(TraceColor));
            Rebuild();
        }
    }

    public int GridColorIndex
    {
        get => _gridColorIndex;
        set
        {
            if (SetProperty(ref _gridColorIndex, Math.Clamp(value, 0, ColorChoiceValues.Length - 1))) Rebuild();
        }
    }

    public int TextColorIndex
    {
        get => _textColorIndex;
        set
        {
            if (SetProperty(ref _textColorIndex, Math.Clamp(value, 0, ColorChoiceValues.Length - 1))) Rebuild();
        }
    }

    public int HeaderPresetIndex
    {
        get => _headerPresetIndex;
        set
        {
            if (!SetProperty(ref _headerPresetIndex, Math.Clamp(value, 0, HeaderPresetChoices.Length - 1)))
                return;

            RaisePropertyChanged(nameof(IsCustomHeader));
            RaisePropertyChanged(nameof(IsPoemHeader));
            Rebuild();
        }
    }

    public bool IsCustomHeader => HeaderPresetIndex == (int)SheetHeaderPreset.Custom;

    public bool IsPoemHeader => HeaderPresetIndex != (int)SheetHeaderPreset.None &&
                                (HeaderPresetIndex == (int)SheetHeaderPreset.Poem || ShowPoemHeader);

    public bool IsTraceMode => PracticeModeIndex == (int)PracticeMode.Trace;

    public FontOption? SelectedSheetFont
    {
        get => _selectedSheetFont;
        set
        {
            if (SetProperty(ref _selectedSheetFont, value)) Rebuild();
        }
    }

    public string HeaderTextTemplate
    {
        get => _headerTextTemplate;
        set
        {
            if (SetProperty(ref _headerTextTemplate, value)) Rebuild();
        }
    }

    public int TextEntryIndex
    {
        get => _textEntryIndex;
        set
        {
            if (SetProperty(ref _textEntryIndex, value) && value >= 0 && value < TextEntries.Count)
            {
                var entry = TextEntries[value];
                InputText = entry.Body;
                // 文本库条目自带作者/朝代时自动带入题头
                if (!string.IsNullOrWhiteSpace(entry.Author))
                {
                    Author = entry.Author;
                    if (!string.IsNullOrWhiteSpace(entry.Dynasty)) Dynasty = entry.Dynasty;
                    ShowPoemHeader = true;
                }
            }
        }
    }

    public bool IsVertical
    {
        get => _isVertical;
        set
        {
            if (SetProperty(ref _isVertical, value)) Rebuild();
        }
    }

    public bool ShowPoemHeader
    {
        get => _showPoemHeader;
        set
        {
            if (!SetProperty(ref _showPoemHeader, value))
                return;

            RaisePropertyChanged(nameof(IsPoemHeader));
            Rebuild();
        }
    }

    public bool FrameBorder
    {
        get => _frameBorder;
        set
        {
            if (SetProperty(ref _frameBorder, value)) Rebuild();
        }
    }

    public string Author
    {
        get => _author;
        set
        {
            if (SetProperty(ref _author, value)) Rebuild();
        }
    }

    public string Dynasty
    {
        get => _dynasty;
        set
        {
            if (SetProperty(ref _dynasty, value)) Rebuild();
        }
    }

    public bool GroupByWord
    {
        get => _groupByWord;
        set
        {
            if (SetProperty(ref _groupByWord, value)) Rebuild();
        }
    }

    public bool ShowPinyin
    {
        get => _showPinyin;
        set
        {
            if (SetProperty(ref _showPinyin, value)) Rebuild();
        }
    }

    public bool PinyinOnly
    {
        get => _pinyinOnly;
        set
        {
            if (SetProperty(ref _pinyinOnly, value)) Rebuild();
        }
    }

    public string InputText
    {
        get => _inputText;
        set
        {
            if (SetProperty(ref _inputText, value)) Rebuild();
        }
    }

    /// <summary>描红字颜色（#RRGGBB），浅色适合打印后手描。</summary>
    public string TraceColor
    {
        get => _traceColor;
        set
        {
            if (SetProperty(ref _traceColor, value)) Rebuild();
        }
    }

    public string Title
    {
        get => _title;
        set
        {
            if (SetProperty(ref _title, value)) Rebuild();
        }
    }

    public int GridKindIndex
    {
        get => _gridKindIndex;
        set
        {
            if (SetProperty(ref _gridKindIndex, value)) Rebuild();
        }
    }

    public int PracticeModeIndex
    {
        get => _practiceModeIndex;
        set
        {
            if (!SetProperty(ref _practiceModeIndex, value))
                return;

            RaisePropertyChanged(nameof(IsTraceMode));
            Rebuild();
        }
    }

    public int RepeatsIndex
    {
        get => _repeatsIndex;
        set
        {
            if (SetProperty(ref _repeatsIndex, value)) Rebuild();
        }
    }

    /// <summary>格子边长（毫米），大字帖调大。</summary>
    public double GridSizeMm
    {
        get => _gridSizeMm;
        set
        {
            if (SetProperty(ref _gridSizeMm, Math.Clamp(value, 8, 60))) Rebuild();
        }
    }

    public bool HollowGlyph
    {
        get => _hollowGlyph;
        set
        {
            if (SetProperty(ref _hollowGlyph, value)) Rebuild();
        }
    }

    /// <summary>纸张模板索引：0 白底、1 红格纸、2 信纸。</summary>
    public int BackgroundIndex
    {
        get => _backgroundIndex;
        set
        {
            if (SetProperty(ref _backgroundIndex, value)) Rebuild();
        }
    }

    public double Zoom
    {
        get => _zoom;
        set => SetProperty(ref _zoom, value);
    }

    public int PageIndex
    {
        get => _pageIndex;
        set
        {
            if (SetProperty(ref _pageIndex, Math.Clamp(value, 0, Math.Max(0, PageCount - 1))))
                RaisePropertyChanged(nameof(PageIndicator));
        }
    }

    public CharacterSheetSpec Spec
    {
        get => _spec;
        private set => SetProperty(ref _spec, value);
    }

    public IReadOnlyList<SheetPage> Pages
    {
        get => _pages;
        private set => SetProperty(ref _pages, value);
    }

    public int PageCount => Pages.Count;

    public string PageIndicator => PageCount == 0
        ? "暂无内容"
        : $"第 {PageIndex + 1} / {PageCount} 页";

    public DelegateCommand GoBackCommand { get; }

    public DelegateCommand PreviousPageCommand { get; }

    public DelegateCommand NextPageCommand { get; }

    public void ExportPdfTo(string path)
    {
        SheetExporter.ExportPdf(path, Spec, Pages);
        ZitieLogging.Info($"已导出 PDF：{path}");
    }

    public void ExportPngTo(string path)
    {
        if (PageIndex < 0 || PageIndex >= Pages.Count) return;

        SheetExporter.ExportPng(path, Spec, Pages[PageIndex], PageCount);
        ZitieLogging.Info($"已导出 PNG：{path}");
    }

    public bool CanExport => PageCount > 0;

    private void Rebuild()
    {
        var headerPreset = (SheetHeaderPreset)Math.Clamp(HeaderPresetIndex, 0, HeaderPresetChoices.Length - 1);
        var traceIntensity = (TraceIntensity)Math.Clamp(TraceIntensityIndex, 0, TraceIntensityChoices.Length - 1);
        var title = string.IsNullOrWhiteSpace(Title) ? null : Title.Trim();
        var showTitle = headerPreset is SheetHeaderPreset.TitleAndFields
                        or SheetHeaderPreset.Poem
                        or SheetHeaderPreset.Custom;
        var showHeaderFields = headerPreset is SheetHeaderPreset.Fields
                               or SheetHeaderPreset.TitleAndFields
                               or SheetHeaderPreset.Custom;

        var spec = new CharacterSheetSpec
        {
            Text = InputText,
            Title = showTitle ? title : null,
            HeaderPreset = headerPreset,
            HeaderTextTemplate = headerPreset == SheetHeaderPreset.Custom &&
                                 !string.IsNullOrWhiteSpace(HeaderTextTemplate)
                ? HeaderTextTemplate.Trim()
                : null,
            CharactersPerLine = CharactersPerLineChoices[
                Math.Clamp(CharactersPerLineIndex, 0, CharactersPerLineChoices.Length - 1)],
            BlankLineCount = BlankLineCount,
            Grid = (GridKind)GridKindIndex,
            Mode = (PracticeMode)PracticeModeIndex,
            RepeatsPerChar = RepeatsChoices[Math.Clamp(RepeatsIndex, 0, RepeatsChoices.Length - 1)],
            TraceSlotCount = _traceSlotCount,
            GridSizeMm = GridSizeMm,
            HollowGlyph = _hollowGlyph,
            GroupByWord = _groupByWord,
            ShowPinyin = _showPinyin,
            PinyinOnly = _pinyinOnly,
            PinyinByGlyph = _showPinyin ? _pinyinCatalog.PinyinByGlyph : null,
            Orientation = _isVertical ? SheetOrientation.Vertical : SheetOrientation.Horizontal,
            ShowPoemHeader = headerPreset == SheetHeaderPreset.Poem ||
                             (headerPreset != SheetHeaderPreset.None && _showPoemHeader),
            ShowHeaderFields = showHeaderFields,
            FrameBorder = _frameBorder,
            BackgroundColor = _backgroundColor,
            BackgroundLineColor = _backgroundLineColor,
            BackgroundLineSpacingMm = _backgroundLineSpacingMm,
            Author = string.IsNullOrWhiteSpace(_author) ? null : _author.Trim(),
            Dynasty = string.IsNullOrWhiteSpace(_dynasty) ? null : _dynasty.Trim(),
            TraceColor = string.IsNullOrWhiteSpace(_traceColor) ? null : _traceColor,
            TraceIntensity = traceIntensity,
            GridColor = ColorChoiceValues[Math.Clamp(GridColorIndex, 0, ColorChoiceValues.Length - 1)],
            TextColor = ColorChoiceValues[Math.Clamp(TextColorIndex, 0, ColorChoiceValues.Length - 1)],
            FontFamilyName = SelectedSheetFont?.Name,
            Background = (SheetBackground)Math.Clamp(_backgroundIndex, 0, 3)
        };

        Spec = spec;
        Pages = LayoutEngine.Paginate(spec);

        if (PageIndex > PageCount - 1) PageIndex = Math.Max(0, PageCount - 1);

        RaisePropertyChanged(nameof(PageCount));
        RaisePropertyChanged(nameof(PageIndicator));
        RaisePropertyChanged(nameof(CanExport));
    }

    private void ApplyModuleDefaults(ModuleDefinition module)
    {
        _module = module;
        if (module.Defaults is not { ValueKind: JsonValueKind.Object } defaults) return;

        var hasHeaderPreset = defaults.TryGetProperty("headerPreset", out var headerPresetValue) &&
                              headerPresetValue.ValueKind == JsonValueKind.String;

        foreach (var property in defaults.EnumerateObject())
            switch (property.Name.ToLowerInvariant())
            {
                case "grid" when property.Value.GetString() is { } grid:
                    GridKindIndex = grid.ToLowerInvariant() switch
                    {
                        "mi" => 0,
                        "tian" => 1,
                        "huigong" => 2,
                        "plain" => 3,
                        "english" => 4,
                        "nine" => 5,
                        "pinyin" => 6,
                        _ => GridKindIndex
                    };
                    break;
                case "gridsize" when property.Value.ValueKind == System.Text.Json.JsonValueKind.Number:
                    GridSizeMm = property.Value.GetDouble();
                    break;
                case "hollowglyph" when property.Value.GetBooleanValue(out var hollowGlyph):
                    HollowGlyph = hollowGlyph;
                    break;
                case "mode" when property.Value.GetString() is { } mode:
                    PracticeModeIndex = mode.ToLowerInvariant() switch
                    {
                        "trace" => 0,
                        "copy" => 1,
                        _ => PracticeModeIndex
                    };
                    break;
                case "groupbyword" when property.Value.GetBooleanValue(out var groupByWord):
                    GroupByWord = groupByWord;
                    break;
                case "showpinyin" when property.Value.GetBooleanValue(out var showPinyin):
                    ShowPinyin = showPinyin;
                    break;
                case "pinyinonly" when property.Value.GetBooleanValue(out var pinyinOnly):
                    PinyinOnly = pinyinOnly;
                    break;
                case "vertical" when property.Value.GetBooleanValue(out var vertical):
                    IsVertical = vertical;
                    break;
                case "showpoemheader" when property.Value.GetBooleanValue(out var showPoemHeader):
                    ShowPoemHeader = showPoemHeader;
                    break;
                case "frameborder" when property.Value.GetBooleanValue(out var frameBorder):
                    FrameBorder = frameBorder;
                    break;
                case "background" when property.Value.GetString() is { } background:
                    BackgroundIndex = background.ToLowerInvariant() switch
                    {
                        "redgrid" => 1,
                        "letter" => 2,
                        "ricepaper" => 3,
                        _ => BackgroundIndex
                    };
                    break;
                case "backgroundcolor" when property.Value.GetString() is { } backgroundColor
                                            && Color.TryParse(backgroundColor, out _):
                    _backgroundColor = backgroundColor;
                    break;
                case "backgroundlinecolor" when property.Value.GetString() is { } backgroundLineColor
                                                && Color.TryParse(backgroundLineColor, out _):
                    _backgroundLineColor = backgroundLineColor;
                    break;
                case "backgroundlinespacing" when property.Value.TryGetDouble(out var backgroundLineSpacing):
                    _backgroundLineSpacingMm = Math.Clamp(backgroundLineSpacing, 1, 60);
                    break;
                case "author" when property.Value.GetString() is { } author:
                    Author = author;
                    break;
                case "dynasty" when property.Value.GetString() is { } dynasty:
                    Dynasty = dynasty;
                    break;
                case "repeats" when property.Value.TryGetInt32(out var repeats):
                {
                    var index = Array.IndexOf(RepeatsChoices, repeats);
                    if (index >= 0) RepeatsIndex = index;
                    break;
                }
                case "tracecount" when property.Value.TryGetInt32(out var traceCount):
                    _traceSlotCount = Math.Clamp(traceCount, 0, 8);
                    break;
                case "title" when property.Value.GetString() is { } title:
                    Title = title;
                    break;
                case "tracecolor" when property.Value.GetString() is { } traceColor
                                       && Color.TryParse(traceColor, out _):
                    TraceColor = traceColor;
                    break;
                case "traceintensity" when property.Value.GetString() is { } traceIntensity:
                    TraceIntensityIndex = traceIntensity.ToLowerInvariant() switch
                    {
                        "verydark" => 0,
                        "dark" => 1,
                        "mediumdark" => 2,
                        "medium" => 3,
                        "light" => 4,
                        "verylight" => 5,
                        "white" => 6,
                        "hollow" => 7,
                        _ => TraceIntensityIndex
                    };
                    break;
                case "charactersperline" when property.Value.TryGetInt32(out var charactersPerLine):
                {
                    var index = Array.IndexOf(CharactersPerLineChoices, charactersPerLine);
                    if (index >= 0) CharactersPerLineIndex = index;
                    break;
                }
                case "blanklinecount" when property.Value.TryGetInt32(out var blankLineCount):
                    BlankLineCount = blankLineCount;
                    break;
                case "gridcolor" when property.Value.GetString() is { } gridColor
                                      && Color.TryParse(gridColor, out _):
                    GridColorIndex = ColorIndexOf(gridColor, GridColorIndex);
                    break;
                case "textcolor" when property.Value.GetString() is { } textColor
                                      && Color.TryParse(textColor, out _):
                    TextColorIndex = ColorIndexOf(textColor, TextColorIndex);
                    break;
                case "fontfamily" when property.Value.GetString() is { } fontFamily:
                    SelectedSheetFont = _fontCatalog.Find(fontFamily) ??
                                        new FontOption(fontFamily.Trim(), false);
                    break;
                case "headerpreset" when property.Value.GetString() is { } headerPreset:
                    HeaderPresetIndex = headerPreset.ToLowerInvariant() switch
                    {
                        "none" => 0,
                        "fields" => 1,
                        "titleandfields" => 2,
                        "poem" => 3,
                        "custom" => 4,
                        _ => HeaderPresetIndex
                    };
                    break;
                case "headertext" when property.Value.GetString() is { } headerText:
                    HeaderTextTemplate = headerText;
                    break;
                case "text" when property.Value.GetString() is { } text:
                    InputText = text;
                    break;
            }

        // 旧模板用 showPoemHeader 表示诗词题头，升级后统一映射到“诗词题头”预设。
        if (!hasHeaderPreset && _showPoemHeader)
            HeaderPresetIndex = (int)SheetHeaderPreset.Poem;
    }

    private static int ColorIndexOf(string color, int fallback)
    {
        var index = Array.FindIndex(ColorChoiceValues, item =>
            string.Equals(item, color, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index : fallback;
    }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        ResetEditorState();
        var module = _catalog.Find(navigationContext.Parameters.GetValue<string?>("moduleId"));
        if (module is not null) ApplyModuleDefaults(module);

        Rebuild();
    }

    private void ResetEditorState()
    {
        _inputText = "床前明月光，疑是地上霜。举头望明月，低头思故乡。";
        _title = string.Empty;
        _gridKindIndex = 0;
        _practiceModeIndex = 0;
        _repeatsIndex = 4;
        _charactersPerLineIndex = 0;
        _blankLineCount = 0;
        _traceIntensityIndex = 3;
        _gridColorIndex = 0;
        _textColorIndex = 1;
        _headerPresetIndex = 2;
        _traceSlotCount = 2;
        _gridSizeMm = 14;
        _hollowGlyph = false;
        _groupByWord = false;
        _showPinyin = false;
        _pinyinOnly = false;
        _isVertical = false;
        _showPoemHeader = false;
        _frameBorder = false;
        _backgroundIndex = 0;
        _backgroundColor = null;
        _backgroundLineColor = null;
        _backgroundLineSpacingMm = null;
        _author = string.Empty;
        _dynasty = string.Empty;
        _selectedSheetFont = _fontCatalog.Find(_fontCatalog.DefaultFontFamily) ??
                             _fontCatalog.Fonts.FirstOrDefault();
        _headerTextTemplate = "姓名_班级---年_月_日";
        _textEntryIndex = -1;
        _zoom = 1.0;
        _pageIndex = 0;
        _traceColor = string.Empty;
        _module = null;

        RaisePropertyChanged(nameof(InputText));
        RaisePropertyChanged(nameof(Title));
        RaisePropertyChanged(nameof(GridKindIndex));
        RaisePropertyChanged(nameof(PracticeModeIndex));
        RaisePropertyChanged(nameof(IsTraceMode));
        RaisePropertyChanged(nameof(RepeatsIndex));
        RaisePropertyChanged(nameof(CharactersPerLineIndex));
        RaisePropertyChanged(nameof(BlankLineCount));
        RaisePropertyChanged(nameof(TraceIntensityIndex));
        RaisePropertyChanged(nameof(GridColorIndex));
        RaisePropertyChanged(nameof(TextColorIndex));
        RaisePropertyChanged(nameof(HeaderPresetIndex));
        RaisePropertyChanged(nameof(IsCustomHeader));
        RaisePropertyChanged(nameof(IsPoemHeader));
        RaisePropertyChanged(nameof(GridSizeMm));
        RaisePropertyChanged(nameof(HollowGlyph));
        RaisePropertyChanged(nameof(GroupByWord));
        RaisePropertyChanged(nameof(ShowPinyin));
        RaisePropertyChanged(nameof(PinyinOnly));
        RaisePropertyChanged(nameof(IsVertical));
        RaisePropertyChanged(nameof(ShowPoemHeader));
        RaisePropertyChanged(nameof(FrameBorder));
        RaisePropertyChanged(nameof(BackgroundIndex));
        RaisePropertyChanged(nameof(Author));
        RaisePropertyChanged(nameof(Dynasty));
        RaisePropertyChanged(nameof(SelectedSheetFont));
        RaisePropertyChanged(nameof(HeaderTextTemplate));
        RaisePropertyChanged(nameof(TextEntryIndex));
        RaisePropertyChanged(nameof(Zoom));
        RaisePropertyChanged(nameof(PageIndex));
        RaisePropertyChanged(nameof(TraceColor));
    }

    public bool IsNavigationTarget(NavigationContext navigationContext)
    {
        return true;
    }

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
    }
}

/// <summary>JsonElement 布尔取值辅助（System.Text.Json 无 TryGetBoolean）。</summary>
internal static class JsonElementBooleanExtensions
{
    public static bool GetBooleanValue(this System.Text.Json.JsonElement element, out bool value)
    {
        if (element.ValueKind == System.Text.Json.JsonValueKind.True ||
            element.ValueKind == System.Text.Json.JsonValueKind.False)
        {
            value = element.GetBoolean();
            return true;
        }

        value = false;
        return false;
    }
}
