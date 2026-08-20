using System.Text.Json;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using Avalonia.Threading;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Zitie.Avalonia.Export;
using Zitie.Core.Layout;
using Zitie.Core.Models;
using Zitie.Desktop.Commands;
using Zitie.Desktop.Services;
using Zitie.Desktop.Models;

namespace Zitie.Desktop.ViewModels;

/// <summary>
///     字帖编辑页：左侧配置驱动 <see cref="Rebuild" /> 重新排版，右侧预览消费同一份结果。
/// </summary>
public class SheetEditorViewModel : BindableBase, INavigationAware
{
    public const double MinimumZoom = 0.3;
    public const double MaximumZoom = 2;
    public const double DefaultZoom = 0.5;

    private readonly ModuleCatalog _catalog;
    private readonly PinyinCatalog _pinyinCatalog;
    private readonly FontCatalog _fontCatalog;
    private readonly IRegionNavigationJournal _journal;
    private readonly ISystemDialogs _dialogs;

    private SheetEditorState _editorState = SheetEditorState.CreateDefault();
    private FontOption? _selectedSheetFont;
    private string _inputTextSummary = string.Empty;
    private double _zoom = DefaultZoom;
    private int _settingsTabIndex;
    private int _pageIndex;
    private string? _documentPath;
    private string _statusMessage = "已就绪";
    private bool _isDirty;
    private bool _suppressDirty;
    private bool _suppressRebuild;
    private bool _isRebuildQueued;
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
        IRegionNavigationJournal journal,
        ISystemDialogs systemDialogs)
    {
        _catalog = catalog;
        _pinyinCatalog = pinyinCatalog;
        _fontCatalog = fontCatalog;
        _journal = journal;
        _dialogs = systemDialogs;
        _selectedSheetFont = _fontCatalog.Find(_fontCatalog.DefaultFontFamily) ??
                             _fontCatalog.Fonts.FirstOrDefault();
        _editorState = SheetEditorState.CreateDefault(_selectedSheetFont?.Name);
        ContentSelection = new TextContentSelection(textCatalog.Entries);
        ContentSelection.EntrySelected += OnTextEntrySelected;

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
        OpenDocumentCommand = new AsyncDelegateCommand(OpenDocumentAsync);
        SaveDocumentCommand = new AsyncDelegateCommand(SaveDocumentAsync);
        SaveAsDocumentCommand = new AsyncDelegateCommand(SaveDocumentAsAsync);
        SaveTemplateCommand = new AsyncDelegateCommand(SaveTemplateAsync);
        ExportPdfCommand = new AsyncDelegateCommand(ExportPdfAsync);
        ExportPngCommand = new AsyncDelegateCommand(ExportPngAsync);
        ExportPngsCommand = new AsyncDelegateCommand(ExportPngsAsync);
    }

    private async Task OpenDocumentAsync()
    {
        try
        {
            var path = await _dialogs.PickOpenFileAsync(
                "字帖文档", ["*.zitie.json", "*.json"], "打开字帖文档");
            if (path is null) return;
            LoadDocumentFrom(path);
        }
        catch (Exception exception)
        {
            SetFailure("打开文档失败", exception);
        }
    }

    private async Task SaveDocumentAsync()
    {
        if (string.IsNullOrWhiteSpace(DocumentPath))
        {
            await SaveDocumentAsAsync();
            return;
        }

        try
        {
            SaveDocumentTo(DocumentPath);
        }
        catch (Exception exception)
        {
            SetFailure("保存文档失败", exception);
        }
    }

    private async Task SaveDocumentAsAsync()
    {
        try
        {
            var path = await _dialogs.PickSaveFileAsync("字帖文档", "zitie.json", "未命名字帖.zitie.json");
            if (path is null) return;
            SaveDocumentTo(path);
        }
        catch (Exception exception)
        {
            SetFailure("保存文档失败", exception);
        }
    }

    private async Task SaveTemplateAsync()
    {
        try
        {
            var path = await _dialogs.PickSaveFileAsync("字帖模板", "json", "我的字帖模板.json");
            if (path is null) return;
            SaveTemplateTo(path);
        }
        catch (Exception exception)
        {
            SetFailure("保存模板失败", exception);
        }
    }

    private async Task ExportPdfAsync()
    {
        if (!CanExport) return;
        try
        {
            var path = await _dialogs.PickSaveFileAsync(
                "PDF 文档", "pdf", $"zitie-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
            if (path is null) return;
            ExportPdfTo(path);
            _dialogs.RevealFile(path);
        }
        catch (Exception exception)
        {
            SetFailure("导出 PDF 失败", exception);
        }
    }

    private async Task ExportPngAsync()
    {
        if (!CanExport) return;
        try
        {
            var path = await _dialogs.PickSaveFileAsync(
                "PNG 图片", "png", $"zitie-{DateTime.Now:yyyyMMdd-HHmmss}.png");
            if (path is null) return;
            ExportPngTo(path);
            _dialogs.RevealFile(path);
        }
        catch (Exception exception)
        {
            SetFailure("导出 PNG 失败", exception);
        }
    }

    private async Task ExportPngsAsync()
    {
        if (!CanExport) return;
        try
        {
            var directory = await _dialogs.PickFolderAsync("选择 PNG 输出目录");
            if (directory is null) return;
            ExportPngsTo(directory);
            _dialogs.OpenFolder(directory);
        }
        catch (Exception exception)
        {
            SetFailure("批量导出 PNG 失败", exception);
        }
    }

    private void SetFailure(string message, Exception exception)
    {
        SetStatus($"{message}：{exception.Message}");
        ZitieLogging.Error(message, exception);
    }

    public int[] RepeatsChoices { get; } = Enumerable.Range(1, 8).ToArray();

    public string[] CharactersPerLineChoices { get; } = ["自动", "12", "16"];

    private static readonly int[] CharactersPerLineValues = [0, 12, 16];

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

    public string[] PageSizeChoices { get; } =
    [
        "A4 纵向",
        "A4 横向",
        "A3 纵向",
        "Letter 纵向"
    ];

    public IReadOnlyList<FontOption> SheetFonts => _fontCatalog.Fonts;

    public TextContentSelection ContentSelection { get; }

    public int CharactersPerLineIndex
    {
        get
        {
            var index = Array.IndexOf(CharactersPerLineValues, _editorState.CharactersPerLine);
            return index < 0 ? 0 : index;
        }
        set
        {
            var index = Math.Clamp(value, 0, CharactersPerLineValues.Length - 1);
            SetEditorState(_editorState with { CharactersPerLine = CharactersPerLineValues[index] });
        }
    }

    public int BlankLineCount
    {
        get => _editorState.BlankLineCount;
        set => SetEditorState(_editorState with { BlankLineCount = Math.Clamp(value, 0, 10) });
    }

    public int TraceIntensityIndex
    {
        get => (int)_editorState.TraceIntensity;
        set
        {
            var intensity = (TraceIntensity)Math.Clamp(value, 0, TraceIntensityChoices.Length - 1);
            if (!SetEditorState(_editorState with { TraceIntensity = intensity, TraceColor = null }))
                return;

            RaisePropertyChanged(nameof(TraceColor));
        }
    }

    public int GridColorIndex
    {
        get => ColorIndexOf(_editorState.GridColor, -1);
        set
        {
            var index = Math.Clamp(value, 0, ColorChoiceValues.Length - 1);
            if (SetEditorState(_editorState with { GridColor = ColorChoiceValues[index] }))
                RaisePropertyChanged(nameof(GridColorHex));
        }
    }

    public int TextColorIndex
    {
        get => ColorIndexOf(_editorState.TextColor, -1);
        set
        {
            var index = Math.Clamp(value, 0, ColorChoiceValues.Length - 1);
            if (SetEditorState(_editorState with { TextColor = ColorChoiceValues[index] }))
                RaisePropertyChanged(nameof(TextColorHex));
        }
    }

    public int HeaderPresetIndex
    {
        get => (int)_editorState.HeaderPreset;
        set
        {
            var preset = (SheetHeaderPreset)Math.Clamp(value, 0, HeaderPresetChoices.Length - 1);
            if (!SetEditorState(_editorState with { HeaderPreset = preset }))
                return;

            RaisePropertyChanged(nameof(IsCustomHeader));
            RaisePropertyChanged(nameof(IsPoemHeader));
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
            if (!SetProperty(ref _selectedSheetFont, value)) return;
            _editorState = _editorState with { FontFamilyName = value?.Name };
            RequestRebuild();
        }
    }

    public string HeaderTextTemplate
    {
        get => _editorState.HeaderTextTemplate;
        set => SetEditorState(_editorState with { HeaderTextTemplate = value ?? string.Empty });
    }

    public bool IsVertical
    {
        get => _editorState.Orientation == SheetOrientation.Vertical;
        set => SetEditorState(_editorState with
        {
            Orientation = value ? SheetOrientation.Vertical : SheetOrientation.Horizontal
        });
    }

    public bool ShowPoemHeader
    {
        get => _editorState.ShowPoemHeader;
        set
        {
            if (!SetEditorState(_editorState with { ShowPoemHeader = value })) return;

            RaisePropertyChanged(nameof(IsPoemHeader));
        }
    }

    public bool FrameBorder
    {
        get => _editorState.FrameBorder;
        set => SetEditorState(_editorState with { FrameBorder = value });
    }

    public string Author
    {
        get => _editorState.Author ?? string.Empty;
        set => SetEditorState(_editorState with { Author = value });
    }

    public string Dynasty
    {
        get => _editorState.Dynasty ?? string.Empty;
        set => SetEditorState(_editorState with { Dynasty = value });
    }

    public bool GroupByWord
    {
        get => _editorState.GroupByWord;
        set => SetEditorState(_editorState with { GroupByWord = value });
    }

    public bool ShowPinyin
    {
        get => _editorState.ShowPinyin;
        set => SetEditorState(_editorState with { ShowPinyin = value });
    }

    public bool PinyinOnly
    {
        get => _editorState.PinyinOnly;
        set => SetEditorState(_editorState with { PinyinOnly = value });
    }

    public string InputText
    {
        get => _editorState.InputText;
        set => SetEditorState(_editorState with { InputText = value ?? string.Empty });
    }

    /// <summary>练习文本字数摘要（如“20 字”），空文本时为空字符串。</summary>
    public string InputTextSummary
    {
        get => _inputTextSummary;
        private set => SetProperty(ref _inputTextSummary, value);
    }

    /// <summary>描红字颜色（#RRGGBB），浅色适合打印后手描。</summary>
    public string TraceColor
    {
        get => _editorState.TraceColor ?? string.Empty;
        set => SetEditorState(_editorState with { TraceColor = value });
    }

    private void OnTextEntrySelected(object? sender, TextEntry entry)
    {
        InputText = entry.Body;
        Title = entry.Title;
        Author = entry.Author;
        Dynasty = entry.Dynasty;
        ShowPoemHeader = !string.IsNullOrWhiteSpace(entry.Author);
    }

    public string GridColorHex
    {
        get => _editorState.GridColor;
        set
        {
            value ??= string.Empty;
            if (SetEditorState(_editorState with { GridColor = value }))
                RaisePropertyChanged(nameof(GridColorIndex));
        }
    }

    public string TextColorHex
    {
        get => _editorState.TextColor;
        set
        {
            value ??= string.Empty;
            if (SetEditorState(_editorState with { TextColor = value }))
                RaisePropertyChanged(nameof(TextColorIndex));
        }
    }

    public string Title
    {
        get => _editorState.Title;
        set => SetEditorState(_editorState with { Title = value ?? string.Empty });
    }

    public int GridKindIndex
    {
        get => (int)_editorState.Grid;
        set => SetEditorState(_editorState with { Grid = (GridKind)Math.Clamp(value, 0, 6) });
    }

    public int PracticeModeIndex
    {
        get => (int)_editorState.Mode;
        set
        {
            if (!SetEditorState(_editorState with { Mode = (PracticeMode)Math.Clamp(value, 0, 1) })) return;

            RaisePropertyChanged(nameof(IsTraceMode));
        }
    }

    public int RepeatsIndex
    {
        get
        {
            var index = Array.IndexOf(RepeatsChoices, _editorState.RepeatsPerChar);
            return index < 0 ? 0 : index;
        }
        set
        {
            var index = Math.Clamp(value, 0, RepeatsChoices.Length - 1);
            SetEditorState(_editorState with { RepeatsPerChar = RepeatsChoices[index] });
        }
    }

    /// <summary>格子边长（毫米），大字帖调大。</summary>
    public double GridSizeMm
    {
        get => _editorState.GridSizeMm;
        set => SetEditorState(_editorState with { GridSizeMm = Math.Clamp(value, 8, 60) });
    }

    public bool HollowGlyph
    {
        get => _editorState.HollowGlyph;
        set => SetEditorState(_editorState with { HollowGlyph = value });
    }

    /// <summary>纸张模板索引：0 白底、1 红格纸、2 信纸。</summary>
    public int BackgroundIndex
    {
        get => (int)_editorState.Background;
        set => SetEditorState(_editorState with { Background = (SheetBackground)Math.Clamp(value, 0, 3) });
    }

    public int PageSizeIndex
    {
        get => PageSizeIndexOf(_editorState.Page);
        set
        {
            var index = Math.Clamp(value, 0, PageSizeChoices.Length - 1);
            SetEditorState(_editorState with { Page = ResolvePageSettings(index, PageMarginMm) });
        }
    }

    public double PageMarginMm
    {
        get => _editorState.Page.MarginTopMm;
        set => SetEditorState(_editorState with
        {
            Page = ResolvePageSettings(PageSizeIndex, Math.Clamp(value, 0, 40))
        });
    }

    public double Zoom
    {
        get => _zoom;
        set => SetProperty(ref _zoom, Math.Clamp(value, MinimumZoom, MaximumZoom));
    }

    public int SettingsTabIndex
    {
        get => _settingsTabIndex;
        set => SetProperty(ref _settingsTabIndex, Math.Clamp(value, 0, 4));
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

    public AsyncDelegateCommand OpenDocumentCommand { get; }

    public AsyncDelegateCommand SaveDocumentCommand { get; }

    public AsyncDelegateCommand SaveAsDocumentCommand { get; }

    public AsyncDelegateCommand SaveTemplateCommand { get; }

    public AsyncDelegateCommand ExportPdfCommand { get; }

    public AsyncDelegateCommand ExportPngCommand { get; }

    public AsyncDelegateCommand ExportPngsCommand { get; }

    public string? DocumentPath
    {
        get => _documentPath;
        private set => SetProperty(ref _documentPath, value);
    }

    public bool IsDirty
    {
        get => _isDirty;
        private set => SetProperty(ref _isDirty, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public void ExportPdfTo(string path)
    {
        FlushPendingRebuild();
        SheetExporter.ExportPdf(path, Spec, Pages);
        SetStatus($"已导出 PDF：{Path.GetFileName(path)}");
        ZitieLogging.Info($"已导出 PDF：{path}");
    }

    public void ExportPngTo(string path)
    {
        FlushPendingRebuild();
        if (PageIndex < 0 || PageIndex >= Pages.Count) return;

        SheetExporter.ExportPng(path, Spec, Pages[PageIndex], PageCount);
        SetStatus($"已导出 PNG：{Path.GetFileName(path)}");
        ZitieLogging.Info($"已导出 PNG：{path}");
    }

    public void ExportPngsTo(string directory, double dpi = 300)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        FlushPendingRebuild();
        Directory.CreateDirectory(directory);
        for (var index = 0; index < Pages.Count; index++)
        {
            var path = Path.Combine(directory, $"zitie-{index + 1:000}.png");
            SheetExporter.ExportPng(path, Spec, Pages[index], PageCount, dpi);
        }

        SetStatus($"已导出 {Pages.Count} 张 PNG");
        ZitieLogging.Info($"已批量导出 PNG：{directory}");
    }

    public void SaveDocumentTo(string path)
    {
        FlushPendingRebuild();
        SheetDocumentStore.Save(path, Spec, _module?.Id);
        DocumentPath = path;
        IsDirty = false;
        SetStatus($"已保存：{Path.GetFileName(path)}");
    }

    public void LoadDocumentFrom(string path)
    {
        var document = SheetDocumentStore.Load(path);
        _suppressDirty = true;
        try
        {
            ApplySpec(document.Spec);
            _module = string.IsNullOrWhiteSpace(document.ModuleId)
                ? null
                : _catalog.Find(document.ModuleId);
        }
        finally
        {
            _suppressDirty = false;
        }

        DocumentPath = path;
        RebuildImmediately();
        IsDirty = false;
        SetStatus($"已打开：{Path.GetFileName(path)}");
    }

    public void SaveTemplateTo(string path, string? name = null)
    {
        FlushPendingRebuild();
        SheetDocumentStore.SaveTemplate(path, Spec, name ?? Title);
        SetStatus($"已保存模板：{Path.GetFileName(path)}");
    }

    public void SetStatus(string message)
    {
        StatusMessage = message;
    }

    public bool CanExport => PageCount > 0;

    private void ApplySpec(CharacterSheetSpec spec)
    {
        _editorState = SheetEditorState.FromSpec(spec);
        _selectedSheetFont = string.IsNullOrWhiteSpace(_editorState.FontFamilyName)
            ? _fontCatalog.Find(_fontCatalog.DefaultFontFamily) ?? _fontCatalog.Fonts.FirstOrDefault()
            : _fontCatalog.Find(_editorState.FontFamilyName) ??
              new FontOption(_editorState.FontFamilyName, false);

        RaisePropertyChanged(string.Empty);
    }

    private bool SetEditorState(
        SheetEditorState state,
        [CallerMemberName] string? propertyName = null)
    {
        if (_editorState == state) return false;

        _editorState = state;
        RaisePropertyChanged(propertyName);
        RequestRebuild();
        return true;
    }

    private void RequestRebuild()
    {
        if (_suppressRebuild || _isRebuildQueued) return;

        _isRebuildQueued = true;
        Dispatcher.UIThread.Post(FlushPendingRebuild, DispatcherPriority.Render);
    }

    private void FlushPendingRebuild()
    {
        if (!_isRebuildQueued || _suppressRebuild) return;

        _isRebuildQueued = false;
        Rebuild();
    }

    private void RebuildImmediately()
    {
        _isRebuildQueued = false;
        Rebuild();
    }

    private void Rebuild()
    {
        var charCount = InputText.Count(char.IsLetterOrDigit);
        InputTextSummary = charCount > 0 ? $"{charCount} 字" : string.Empty;

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
            CharactersPerLine = CharactersPerLineValues[
                Math.Clamp(CharactersPerLineIndex, 0, CharactersPerLineValues.Length - 1)],
            BlankLineCount = BlankLineCount,
            Grid = (GridKind)GridKindIndex,
            Mode = (PracticeMode)PracticeModeIndex,
            RepeatsPerChar = RepeatsChoices[Math.Clamp(RepeatsIndex, 0, RepeatsChoices.Length - 1)],
            TraceSlotCount = _editorState.TraceSlotCount,
            GridSizeMm = GridSizeMm,
            HollowGlyph = HollowGlyph,
            GroupByWord = GroupByWord,
            ShowPinyin = ShowPinyin,
            PinyinOnly = PinyinOnly,
            PinyinByGlyph = ShowPinyin ? _pinyinCatalog.PinyinByGlyph : null,
            Orientation = _editorState.Orientation,
            ShowPoemHeader = headerPreset == SheetHeaderPreset.Poem ||
                             (headerPreset != SheetHeaderPreset.None && ShowPoemHeader),
            ShowHeaderFields = showHeaderFields,
            FrameBorder = FrameBorder,
            BackgroundColor = _editorState.BackgroundColor,
            BackgroundLineColor = _editorState.BackgroundLineColor,
            BackgroundLineSpacingMm = _editorState.BackgroundLineSpacingMm,
            Author = string.IsNullOrWhiteSpace(Author) ? null : Author.Trim(),
            Dynasty = string.IsNullOrWhiteSpace(Dynasty) ? null : Dynasty.Trim(),
            TraceColor = string.IsNullOrWhiteSpace(TraceColor) ? null : TraceColor,
            TraceIntensity = traceIntensity,
            GridColor = GridColorHex,
            TextColor = TextColorHex,
            FontFamilyName = SelectedSheetFont?.Name,
            Background = _editorState.Background,
            Page = _editorState.Page
        };

        Spec = spec;
        Pages = LayoutEngine.Paginate(spec);

        if (PageIndex > PageCount - 1) PageIndex = Math.Max(0, PageCount - 1);

        RaisePropertyChanged(nameof(PageCount));
        RaisePropertyChanged(nameof(PageIndicator));
        RaisePropertyChanged(nameof(CanExport));
        if (!_suppressDirty) IsDirty = true;
    }

    private static PageSettings ResolvePageSettings(int pageSizeIndex, double marginMm)
    {
        var (width, height) = pageSizeIndex switch
        {
            1 => (297d, 210d),
            2 => (297d, 420d),
            3 => (216d, 279d),
            _ => (210d, 297d)
        };

        return new PageSettings
        {
            WidthMm = width,
            HeightMm = height,
            MarginTopMm = marginMm,
            MarginBottomMm = marginMm,
            MarginLeftMm = marginMm,
            MarginRightMm = marginMm
        };
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
                    _editorState = _editorState with { BackgroundColor = backgroundColor };
                    break;
                case "backgroundlinecolor" when property.Value.GetString() is { } backgroundLineColor
                                                && Color.TryParse(backgroundLineColor, out _):
                    _editorState = _editorState with { BackgroundLineColor = backgroundLineColor };
                    break;
                case "backgroundlinespacing" when property.Value.TryGetDouble(out var backgroundLineSpacing):
                    _editorState = _editorState with
                    {
                        BackgroundLineSpacingMm = Math.Clamp(backgroundLineSpacing, 1, 60)
                    };
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
                    _editorState = _editorState with { TraceSlotCount = Math.Clamp(traceCount, 0, 8) };
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
                    var index = Array.IndexOf(CharactersPerLineValues, charactersPerLine);
                    if (index >= 0) CharactersPerLineIndex = index;
                    break;
                }
                case "blanklinecount" when property.Value.TryGetInt32(out var blankLineCount):
                    BlankLineCount = blankLineCount;
                    break;
                case "gridcolor" when property.Value.GetString() is { } gridColor
                                      && Color.TryParse(gridColor, out _):
                    _editorState = _editorState with { GridColor = gridColor };
                    RaisePropertyChanged(nameof(GridColorIndex));
                    RaisePropertyChanged(nameof(GridColorHex));
                    RequestRebuild();
                    break;
                case "textcolor" when property.Value.GetString() is { } textColor
                                      && Color.TryParse(textColor, out _):
                    _editorState = _editorState with { TextColor = textColor };
                    RaisePropertyChanged(nameof(TextColorIndex));
                    RaisePropertyChanged(nameof(TextColorHex));
                    RequestRebuild();
                    break;
                case "pagesize" when property.Value.GetString() is { } pageSize:
                    PageSizeIndex = pageSize.ToLowerInvariant() switch
                    {
                        "a4landscape" => 1,
                        "a3portrait" => 2,
                        "letter" => 3,
                        _ => 0
                    };
                    break;
                case "pagemargin" when property.Value.TryGetDouble(out var pageMargin):
                    PageMarginMm = pageMargin;
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
        if (!hasHeaderPreset && ShowPoemHeader)
            HeaderPresetIndex = (int)SheetHeaderPreset.Poem;
    }

    private static int ColorIndexOf(string color, int fallback)
    {
        var index = Array.FindIndex(ColorChoiceValues, item =>
            string.Equals(item, color, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index : fallback;
    }

    private static int PageSizeIndexOf(PageSettings page)
    {
        if (Math.Abs(page.WidthMm - 297) < 0.1 && Math.Abs(page.HeightMm - 210) < 0.1) return 1;
        if (Math.Abs(page.WidthMm - 297) < 0.1 && Math.Abs(page.HeightMm - 420) < 0.1) return 2;
        if (Math.Abs(page.WidthMm - 216) < 0.1 && Math.Abs(page.HeightMm - 279) < 0.1) return 3;
        return 0;
    }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        _suppressDirty = true;
        _suppressRebuild = true;
        try
        {
            ResetEditorState();
            var module = _catalog.Find(navigationContext.Parameters.GetValue<string?>("moduleId"));
            if (module is not null) ApplyModuleDefaults(module);
        }
        finally
        {
            _suppressRebuild = false;
            _suppressDirty = false;
        }

        RebuildImmediately();
        IsDirty = false;
        SetStatus("已就绪");
    }

    private void ResetEditorState()
    {
        _selectedSheetFont = _fontCatalog.Find(_fontCatalog.DefaultFontFamily) ??
                             _fontCatalog.Fonts.FirstOrDefault();
        _editorState = SheetEditorState.CreateDefault(_selectedSheetFont?.Name);
        ContentSelection.Reset();
        _zoom = DefaultZoom;
        _pageIndex = 0;
        _documentPath = null;
        _statusMessage = "已就绪";
        _isDirty = false;
        _isRebuildQueued = false;
        _module = null;

        RaisePropertyChanged(string.Empty);
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
