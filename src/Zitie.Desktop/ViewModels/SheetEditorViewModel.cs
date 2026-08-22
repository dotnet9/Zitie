using System.Runtime.CompilerServices;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
public class SheetEditorViewModel : BindableBase, INavigationAware, IDisposable
{
    public const double MinimumZoom = 0.3;
    public const double MaximumZoom = 2;
    public const double DefaultZoom = 0.5;

    private readonly ModuleCatalog _catalog;
    private readonly PinyinCatalog _pinyinCatalog;
    private readonly StrokeOrderCatalog _strokeOrderCatalog;
    private readonly FontCatalog _fontCatalog;
    private readonly IRegionNavigationJournal _journal;
    private readonly ISystemDialogs _dialogs;
    private readonly Subject<string> _templateSearchTextChanges = new();
    private readonly CompositeDisposable _subscriptions = new();

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
    private bool _isContentPickerOpen;
    private bool _isTemplatePickerOpen;
    private CharacterSheetSpec _spec = new();
    private IReadOnlyList<SheetPage> _pages = Array.Empty<SheetPage>();
    private IReadOnlyList<ModuleDefinition> _templateChoices = Array.Empty<ModuleDefinition>();
    private IReadOnlyList<ModuleDefinition> _filteredTemplateChoices = Array.Empty<ModuleDefinition>();
    private ModuleDefinition? _module;
    private ModuleDefinition? _previewTemplateModule;
    private EditorPreviewSnapshot? _contentPreviewSnapshot;
    private EditorPreviewSnapshot? _templatePreviewSnapshot;
    private string _templateSearchText = string.Empty;

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
        ISystemDialogs systemDialogs,
        StrokeOrderCatalog? strokeOrderCatalog = null)
    {
        _catalog = catalog;
        _pinyinCatalog = pinyinCatalog;
        _strokeOrderCatalog = strokeOrderCatalog ?? new StrokeOrderCatalog();
        _fontCatalog = fontCatalog;
        _journal = journal;
        _dialogs = systemDialogs;
        _selectedSheetFont = _fontCatalog.Find(_fontCatalog.DefaultFontFamily) ??
                             _fontCatalog.Fonts.FirstOrDefault();
        _editorState = SheetEditorState.CreateDefault(_selectedSheetFont?.Name);
        RefreshTemplateChoices();
        ContentSelection = new TextContentSelection(textCatalog.Entries);

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
        OpenContentPickerCommand = new DelegateCommand(OpenContentPicker);
        CloseContentPickerCommand = new DelegateCommand(() => CloseContentPicker(commit: false));
        ResetContentFiltersCommand = new DelegateCommand(ContentSelection.Reset);
        ApplySelectedContentCommand = new DelegateCommand(
            ApplySelectedContent,
            () => ContentSelection.SelectedEntry is not null);
        ContentSelection.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(TextContentSelection.SelectedEntry))
            {
                ApplySelectedContentCommand.RaiseCanExecuteChanged();
                PreviewSelectedContent();
            }
        };
        OpenTemplatePickerCommand = new DelegateCommand(OpenTemplatePicker);
        CloseTemplatePickerCommand = new DelegateCommand(() => CloseTemplatePicker(commit: false));
        ResetTemplateSearchCommand = new DelegateCommand(ResetTemplateSearch);
        ApplySelectedTemplateCommand = new DelegateCommand(
            ApplySelectedTemplate,
            () => PreviewTemplateModule is not null);
        ZoomOutCommand = new DelegateCommand(
                () => AdjustZoom(-0.1),
                () => Zoom > 0.3)
            .ObservesProperty(() => Zoom);
        ZoomInCommand = new DelegateCommand(
                () => AdjustZoom(0.1),
                () => Zoom < 2)
            .ObservesProperty(() => Zoom);
        ConfigureTemplateSearchDebounce();
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
            var path = await _dialogs.PickSaveFileAsync("字帖模板包", "zi", "我的字帖模板.zi");
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

    public string[] CellsPerLineChoices { get; } = ["自动", "5", "7", "8", "10", "12", "14", "16", "20", "24"];

    private static readonly int[] CellsPerLineValues = [0, 5, 7, 8, 10, 12, 14, 16, 20, 24];

    public int[] BlankCellLineChoices { get; } = Enumerable.Range(0, 11).ToArray();

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

    public IReadOnlyList<ModuleDefinition> TemplateChoices
    {
        get => _templateChoices;
        private set
        {
            if (!SetProperty(ref _templateChoices, value)) return;
            ApplyTemplateFilter();
        }
    }

    public IReadOnlyList<ModuleDefinition> FilteredTemplateChoices
    {
        get => _filteredTemplateChoices;
        private set
        {
            if (!SetProperty(ref _filteredTemplateChoices, value)) return;
            RaisePropertyChanged(nameof(FilteredTemplateChoiceCount));
            RaisePropertyChanged(nameof(TemplateFilterSummary));
            RaisePropertyChanged(nameof(IsTemplateFilterEmpty));
        }
    }

    public int FilteredTemplateChoiceCount => FilteredTemplateChoices.Count;

    public bool IsTemplateFilterEmpty => FilteredTemplateChoices.Count == 0;

    public string TemplateFilterSummary => $"{FilteredTemplateChoiceCount} / {TemplateChoices.Count} 个模板";

    public string TemplateSearchText
    {
        get => _templateSearchText;
        set
        {
            if (!SetProperty(ref _templateSearchText, value?.Trim() ?? string.Empty)) return;
            _templateSearchTextChanges.OnNext(_templateSearchText);
        }
    }

    public ModuleDefinition? PreviewTemplateModule
    {
        get => _previewTemplateModule;
        set
        {
            if (value is { Enabled: false }) return;
            if (SameModule(_previewTemplateModule, value)) return;

            SetPreviewTemplateModule(value);
            PreviewSelectedTemplate();
        }
    }

    public ModuleDefinition? SelectedModule
    {
        get => _module;
        set
        {
            if (value is null || IsCurrentModule(value)) return;
            SwitchTemplate(value);
        }
    }

    public string CurrentTemplateName => _module?.Name ?? "未选择模板";

    public int CellsPerLineIndex
    {
        get
        {
            var index = Array.IndexOf(CellsPerLineValues, _editorState.CellsPerLine);
            return index < 0 ? 0 : index;
        }
        set
        {
            var index = Math.Clamp(value, 0, CellsPerLineValues.Length - 1);
            SetEditorState(_editorState with { CellsPerLine = CellsPerLineValues[index] });
        }
    }

    public int BlankCellLineCount
    {
        get => _editorState.BlankCellLineCount;
        set => SetEditorState(_editorState with { BlankCellLineCount = Math.Clamp(value, 0, 10) });
    }

    public string BlankCellLineLabel => IsVertical ? "空格子列" : "空格子行";

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

    public bool IsStandardPracticeLayout => _editorState.PracticeLayout == PracticeLayoutKind.Standard;

    public bool IsSpecialPracticeLayout => !IsStandardPracticeLayout;

    public bool IsPoemHeaderFieldsEnabled => IsStandardPracticeLayout && IsPoemHeader;

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
        set
        {
            if (!SetEditorState(_editorState with
                {
                    Orientation = value ? SheetOrientation.Vertical : SheetOrientation.Horizontal
                }))
                return;

            RaisePropertyChanged(nameof(BlankCellLineLabel));
        }
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
        set => SetEditorState(_editorState with { InputText = IsContentEditingEnabled ? value ?? string.Empty : string.Empty });
    }

    public bool IsContentEditingEnabled => !_editorState.BlankContentLayout;

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

    private void ApplySelectedContent()
    {
        if (!IsContentEditingEnabled) return;

        var entry = ContentSelection.SelectedEntry;
        if (entry is null) return;

        if (IsContentPickerOpen)
        {
            PreviewContentEntry(entry);
            _contentPreviewSnapshot = null;
            IsDirty = true;
            CloseContentPicker(commit: true);
            SetStatus($"已应用内容：{entry.Title}");
            return;
        }

        ApplyTextEntryToState(entry);
        SetStatus($"已应用内容：{entry.Title}");
    }

    private void ApplyTextEntryToState(TextEntry entry)
    {
        InputText = entry.Body;
        Title = entry.Title;
        Author = entry.Author;
        Dynasty = entry.Dynasty;
        ShowPoemHeader = !string.IsNullOrWhiteSpace(entry.Author);
    }

    private void OpenContentPicker()
    {
        if (!IsContentEditingEnabled) return;
        if (_isContentPickerOpen) return;
        if (_isTemplatePickerOpen) CloseTemplatePicker(commit: false);

        _contentPreviewSnapshot = CaptureEditorPreviewSnapshot();
        SetProperty(ref _isContentPickerOpen, true, nameof(IsContentPickerOpen));
    }

    private void CloseContentPicker(bool commit)
    {
        if (!_isContentPickerOpen) return;

        if (!commit && _contentPreviewSnapshot is { } snapshot)
            RestoreEditorPreviewSnapshot(snapshot);

        _contentPreviewSnapshot = null;
        SetProperty(ref _isContentPickerOpen, false, nameof(IsContentPickerOpen));
    }

    private void PreviewSelectedContent()
    {
        if (!_isContentPickerOpen || _contentPreviewSnapshot is null) return;

        var entry = ContentSelection.SelectedEntry;
        if (entry is null)
        {
            RestoreEditorPreviewSnapshot(_contentPreviewSnapshot);
            return;
        }

        PreviewContentEntry(entry);
    }

    private void PreviewContentEntry(TextEntry entry)
    {
        if (_contentPreviewSnapshot is null) return;

        ApplyPreviewFromSnapshot(
            _contentPreviewSnapshot,
            () => ApplyTextEntryToState(entry));
        SetStatus($"预览内容：{entry.Title}");
    }

    private void OpenTemplatePicker()
    {
        if (_isTemplatePickerOpen) return;
        if (_isContentPickerOpen) CloseContentPicker(commit: false);

        _templatePreviewSnapshot = CaptureEditorPreviewSnapshot();
        SetProperty(ref _isTemplatePickerOpen, true, nameof(IsTemplatePickerOpen));

        var current = FindTemplateChoice(_module);
        if (current is not null)
            SetPreviewTemplateModule(current);
        else
            ClearPreviewTemplateModule();
    }

    private void CloseTemplatePicker(bool commit)
    {
        if (!_isTemplatePickerOpen) return;

        if (!commit && _templatePreviewSnapshot is { } snapshot)
            RestoreEditorPreviewSnapshot(snapshot);

        _templatePreviewSnapshot = null;
        SetProperty(ref _isTemplatePickerOpen, false, nameof(IsTemplatePickerOpen));
        ClearPreviewTemplateModule();
    }

    private void ApplySelectedTemplate()
    {
        var module = PreviewTemplateModule;
        if (module is null) return;

        if (!IsTemplatePickerOpen)
        {
            SwitchTemplate(module);
            return;
        }

        var changed = _templatePreviewSnapshot is null || !SameModule(_templatePreviewSnapshot.Module, module);
        if (changed)
        {
            PreviewTemplate(module);
            IsDirty = true;
        }

        _templatePreviewSnapshot = null;
        CloseTemplatePicker(commit: true);
        SetStatus($"已应用模板：{module.Name}");
    }

    private void PreviewSelectedTemplate()
    {
        if (!_isTemplatePickerOpen || _templatePreviewSnapshot is null) return;

        if (PreviewTemplateModule is null)
        {
            RestoreEditorPreviewSnapshot(_templatePreviewSnapshot);
            return;
        }

        PreviewTemplate(PreviewTemplateModule);
    }

    private void PreviewTemplate(ModuleDefinition module)
    {
        if (_templatePreviewSnapshot is null) return;

        var preservedContent = ContentSnapshot.Capture(_templatePreviewSnapshot.State);
        ApplyPreviewFromSnapshot(
            _templatePreviewSnapshot,
            () => ApplyModuleDefaults(module, preserveContent: true, preservedContent));
        SetStatus($"预览模板：{module.Name}");
    }

    private void ClearPreviewTemplateModule()
    {
        if (_previewTemplateModule is null) return;

        SetPreviewTemplateModule(null);
    }

    private void SetPreviewTemplateModule(ModuleDefinition? module)
    {
        _previewTemplateModule = module;
        RaisePropertyChanged(nameof(PreviewTemplateModule));
        ApplySelectedTemplateCommand.RaiseCanExecuteChanged();
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
        set => SetEditorState(_editorState with { Grid = (GridKind)Math.Clamp(value, 0, 7) });
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

    public double GridGapMm
    {
        get => _editorState.GridGapMm;
        set => SetEditorState(_editorState with { GridGapMm = Math.Clamp(value, 0, 20) });
    }

    /// <summary>每个字的练习组之间、行之间的额外间距。</summary>
    public double GroupGapMm
    {
        get => _editorState.GroupGapMm;
        set => SetEditorState(_editorState with
        {
            GroupGapMm = Math.Round(Math.Clamp(value, 1, 10), 1, MidpointRounding.AwayFromZero)
        });
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

    public void AdjustZoom(double delta)
    {
        Zoom = Math.Round(Zoom + delta, 2, MidpointRounding.AwayFromZero);
    }

    public int SettingsTabIndex
    {
        get => _settingsTabIndex;
        set => SetProperty(ref _settingsTabIndex, Math.Clamp(value, 0, 4));
    }

    public bool IsContentPickerOpen
    {
        get => _isContentPickerOpen;
        set
        {
            if (value)
                OpenContentPicker();
            else
                CloseContentPicker(commit: false);
        }
    }

    public bool IsTemplatePickerOpen
    {
        get => _isTemplatePickerOpen;
        set
        {
            if (value)
                OpenTemplatePicker();
            else
                CloseTemplatePicker(commit: false);
        }
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

    public DelegateCommand OpenContentPickerCommand { get; }

    public DelegateCommand CloseContentPickerCommand { get; }

    public DelegateCommand ResetContentFiltersCommand { get; }

    public DelegateCommand ApplySelectedContentCommand { get; }

    public DelegateCommand OpenTemplatePickerCommand { get; }

    public DelegateCommand CloseTemplatePickerCommand { get; }

    public DelegateCommand ResetTemplateSearchCommand { get; }

    public DelegateCommand ApplySelectedTemplateCommand { get; }

    public DelegateCommand ZoomOutCommand { get; }

    public DelegateCommand ZoomInCommand { get; }

    public string? DocumentPath
    {
        get => _documentPath;
        private set
        {
            if (SetProperty(ref _documentPath, value))
                RaisePropertyChanged(nameof(DocumentDisplayPath));
        }
    }

    public string DocumentDisplayPath => string.IsNullOrWhiteSpace(DocumentPath)
        ? $"未保存文档{(IsDirty ? " *" : string.Empty)}"
        : $"{DocumentPath}{(IsDirty ? " *" : string.Empty)}";

    public bool IsDirty
    {
        get => _isDirty;
        private set
        {
            if (SetProperty(ref _isDirty, value))
                RaisePropertyChanged(nameof(DocumentDisplayPath));
        }
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
            SetCurrentModule(string.IsNullOrWhiteSpace(document.ModuleId)
                ? null
                : _catalog.Find(document.ModuleId));
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
        _catalog.Reload();
        RefreshTemplateChoices();
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
              FontOption.CreateUnclassified(_editorState.FontFamilyName);

        RaisePropertyChanged(string.Empty);
    }

    private EditorPreviewSnapshot CaptureEditorPreviewSnapshot()
    {
        FlushPendingRebuild();

        return new EditorPreviewSnapshot(
            _editorState,
            _selectedSheetFont,
            _module,
            PageIndex,
            IsDirty,
            StatusMessage);
    }

    private void ApplyPreviewFromSnapshot(EditorPreviewSnapshot snapshot, Action applyPreview)
    {
        var previousSuppressRebuild = _suppressRebuild;
        var previousSuppressDirty = _suppressDirty;
        _suppressRebuild = true;
        _suppressDirty = true;
        try
        {
            RestoreEditorPreviewSnapshotCore(snapshot);
            applyPreview();
            RaisePropertyChanged(string.Empty);
        }
        finally
        {
            _suppressRebuild = previousSuppressRebuild;
            _suppressDirty = previousSuppressDirty;
        }

        RebuildPreservingDirty(snapshot.IsDirty);
    }

    private void RestoreEditorPreviewSnapshot(EditorPreviewSnapshot snapshot)
    {
        var previousSuppressRebuild = _suppressRebuild;
        _suppressRebuild = true;
        try
        {
            RestoreEditorPreviewSnapshotCore(snapshot);
        }
        finally
        {
            _suppressRebuild = previousSuppressRebuild;
        }

        RebuildPreservingDirty(snapshot.IsDirty);
    }

    private void RestoreEditorPreviewSnapshotCore(EditorPreviewSnapshot snapshot)
    {
        _editorState = snapshot.State;
        _selectedSheetFont = snapshot.SelectedSheetFont;
        _pageIndex = snapshot.PageIndex;
        _statusMessage = snapshot.StatusMessage;
        SetCurrentModule(snapshot.Module);
        RaisePropertyChanged(string.Empty);
    }

    private void RebuildPreservingDirty(bool isDirty)
    {
        var previousSuppressDirty = _suppressDirty;
        _suppressDirty = true;
        try
        {
            RebuildImmediately();
        }
        finally
        {
            _suppressDirty = previousSuppressDirty;
        }

        IsDirty = isDirty;
    }

    private bool SetEditorState(
        SheetEditorState state,
        [CallerMemberName] string? propertyName = null)
    {
        if (_editorState == state) return false;

        var previous = _editorState;
        _editorState = state;
        RaisePropertyChanged(propertyName);
        if (previous.PracticeLayout != state.PracticeLayout)
        {
            RaisePropertyChanged(nameof(IsStandardPracticeLayout));
            RaisePropertyChanged(nameof(IsSpecialPracticeLayout));
        }

        if (previous.PracticeLayout != state.PracticeLayout ||
            previous.HeaderPreset != state.HeaderPreset ||
            previous.ShowPoemHeader != state.ShowPoemHeader)
        {
            RaisePropertyChanged(nameof(IsPoemHeader));
            RaisePropertyChanged(nameof(IsPoemHeaderFieldsEnabled));
        }

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
        var specialLayoutUsesContentTitle = _editorState.PracticeLayout == PracticeLayoutKind.CharacterWordsPoem;
        var showTitle = headerPreset is SheetHeaderPreset.TitleAndFields
                        or SheetHeaderPreset.Poem
                        or SheetHeaderPreset.Custom;
        var showHeaderFields = headerPreset is SheetHeaderPreset.Fields
                               or SheetHeaderPreset.TitleAndFields
                               or SheetHeaderPreset.Custom;

        var spec = new CharacterSheetSpec
        {
            Text = InputText,
            PracticeLayout = _editorState.PracticeLayout,
            BlankContentLayout = _editorState.BlankContentLayout,
            FillContentAreaWithBlankCells = _editorState.FillContentAreaWithBlankCells,
            Title = showTitle || specialLayoutUsesContentTitle ? title : null,
            HeaderPreset = headerPreset,
            HeaderTextTemplate = headerPreset == SheetHeaderPreset.Custom &&
                                 !string.IsNullOrWhiteSpace(HeaderTextTemplate)
                ? HeaderTextTemplate.Trim()
                : null,
            CellsPerLine = Math.Clamp(_editorState.CellsPerLine, 0, 64),
            LayoutColumns = Math.Clamp(_editorState.LayoutColumns, 0, 64),
            LayoutRows = Math.Clamp(_editorState.LayoutRows, 0, 128),
            BlankCellLineCount = BlankCellLineCount,
            Grid = (GridKind)GridKindIndex,
            Mode = (PracticeMode)PracticeModeIndex,
            RepeatsPerChar = RepeatsChoices[Math.Clamp(RepeatsIndex, 0, RepeatsChoices.Length - 1)],
            TraceSlotCount = _editorState.TraceSlotCount,
            GridSizeMm = GridSizeMm,
            GridGapMm = GridGapMm,
            GroupGapMm = GroupGapMm,
            HollowGlyph = HollowGlyph,
            GroupByWord = GroupByWord,
            ShowPinyin = ShowPinyin,
            PinyinOnly = PinyinOnly,
            ShowStrokeOrder = _editorState.ShowStrokeOrder,
            PinyinByGlyph = ShowPinyin || _editorState.Grid == GridKind.Pinyin
                ? _pinyinCatalog.PinyinByGlyph
                : null,
            StrokeOrderByGlyph = ResolveStrokeOrders(),
            Orientation = _editorState.Orientation,
            ShowPoemHeader = headerPreset == SheetHeaderPreset.Poem ||
                             (headerPreset != SheetHeaderPreset.None && ShowPoemHeader),
            ShowHeaderFields = showHeaderFields,
            FrameBorder = FrameBorder,
            BackgroundColor = _editorState.BackgroundColor,
            BackgroundLineColor = _editorState.BackgroundLineColor,
            BackgroundLineSpacingMm = _editorState.BackgroundLineSpacingMm,
            BackgroundArtwork = _editorState.BackgroundArtwork,
            BackgroundArtworkSvg = _editorState.BackgroundArtworkSvg,
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

    private IReadOnlyDictionary<string, CharacterStrokeOrder>? ResolveStrokeOrders()
    {
        if (!_editorState.ShowStrokeOrder &&
            _editorState.PracticeLayout != PracticeLayoutKind.CharacterWordsPoem)
            return null;

        var glyphs = StrokeOrderSource()
            .Where(IsChinese)
            .Select(static glyph => glyph.ToString());
        var result = _strokeOrderCatalog.FindForGlyphs(glyphs);
        return result.Count > 0 ? result : null;
    }

    private IEnumerable<char> StrokeOrderSource()
    {
        foreach (var ch in InputText)
            yield return ch;
        foreach (var ch in Title ?? string.Empty)
            yield return ch;
        foreach (var ch in Dynasty ?? string.Empty)
            yield return ch;
        foreach (var ch in Author ?? string.Empty)
            yield return ch;
        foreach (var ch in "春冬风雪花")
            yield return ch;
    }

    private static bool IsChinese(char value)
    {
        return value is >= '\u3400' and <= '\u9FFF' ||
               value is >= '\uF900' and <= '\uFAFF';
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

    public void SwitchTemplate(ModuleDefinition module)
    {
        if (!module.Enabled || IsCurrentModule(module)) return;

        var content = ContentSnapshot.Capture(_editorState);
        _suppressRebuild = true;
        try
        {
            ApplyModuleDefaults(module, preserveContent: true, content);
        }
        finally
        {
            _suppressRebuild = false;
        }

        RebuildImmediately();
        IsDirty = true;
        SetStatus($"已切换模板：{module.Name}");
    }

    private void ApplyModuleDefaults(
        ModuleDefinition module,
        bool preserveContent = false,
        ContentSnapshot? preservedContent = null)
    {
        SetCurrentModule(module);
        var defaults = module.Defaults;
        preservedContent ??= preserveContent ? ContentSnapshot.Capture(_editorState) : null;

        var hasHeaderPreset = !string.IsNullOrWhiteSpace(defaults.HeaderPreset);

        var blankContentLayout = defaults.BlankContentLayout == true;
        SetEditorState(_editorState with
        {
            PracticeLayout = PracticeLayoutKindParser.Parse(defaults.PracticeLayout),
            BlankContentLayout = blankContentLayout,
            FillContentAreaWithBlankCells = defaults.FillContentAreaWithBlankCells == true,
            LayoutColumns = Math.Clamp(defaults.LayoutColumns ?? 0, 0, 64),
            LayoutRows = Math.Clamp(defaults.LayoutRows ?? 0, 0, 128)
        }, nameof(IsContentEditingEnabled));
        if (blankContentLayout && _isContentPickerOpen)
            CloseContentPicker(commit: false);

        if (defaults.Grid is { Length: > 0 } grid)
            GridKindIndex = grid.ToLowerInvariant() switch
            {
                "none" => 0,
                "mi" => 1,
                "tian" => 2,
                "huigong" => 3,
                "plain" => 4,
                "english" => 5,
                "nine" => 6,
                "pinyin" => 7,
                _ => GridKindIndex
            };

        if (defaults.GridSize is { } gridSize) GridSizeMm = gridSize;
        if (defaults.GridGap is { } gridGap) GridGapMm = gridGap;
        if (defaults.HollowGlyph is { } hollowGlyph) HollowGlyph = hollowGlyph;
        if (defaults.Mode is { Length: > 0 } mode)
            PracticeModeIndex = mode.ToLowerInvariant() switch
            {
                "trace" => 0,
                "copy" => 1,
                _ => PracticeModeIndex
            };

        if (defaults.GroupByWord is { } groupByWord) GroupByWord = groupByWord;
        if (defaults.ShowPinyin is { } showPinyin) ShowPinyin = showPinyin;
        if (defaults.PinyinOnly is { } pinyinOnly) PinyinOnly = pinyinOnly;
        if (defaults.ShowStrokeOrder is { } showStrokeOrder)
            _editorState = _editorState with { ShowStrokeOrder = showStrokeOrder };
        if (defaults.Vertical is { } vertical) IsVertical = vertical;
        if (defaults.ShowPoemHeader is { } showPoemHeader) ShowPoemHeader = showPoemHeader;
        if (defaults.FrameBorder is { } frameBorder) FrameBorder = frameBorder;

        if (defaults.Background is { Length: > 0 } background)
            BackgroundIndex = background.ToLowerInvariant() switch
            {
                "plain" => 0,
                "redgrid" => 1,
                "letter" => 2,
                "ricepaper" => 3,
                _ => BackgroundIndex
            };

        if (defaults.BackgroundColor is { Length: > 0 } backgroundColor &&
            Color.TryParse(backgroundColor, out _))
            _editorState = _editorState with { BackgroundColor = backgroundColor };
        if (defaults.BackgroundLineColor is { Length: > 0 } backgroundLineColor &&
            Color.TryParse(backgroundLineColor, out _))
            _editorState = _editorState with { BackgroundLineColor = backgroundLineColor };
        if (defaults.BackgroundLineSpacing is { } backgroundLineSpacing)
            _editorState = _editorState with
            {
                BackgroundLineSpacingMm = Math.Clamp(backgroundLineSpacing, 1, 60)
            };
        if (defaults.BackgroundArtwork is { Length: > 0 } backgroundArtwork)
            _editorState = _editorState with
            {
                BackgroundArtwork = backgroundArtwork.Trim(),
                BackgroundArtworkSvg = module.ReadTextAsset(backgroundArtwork)
            };

        if (defaults.Author is { } author) Author = author;
        if (defaults.Dynasty is { } dynasty) Dynasty = dynasty;
        if (defaults.Repeats is { } repeats)
        {
            var index = Array.IndexOf(RepeatsChoices, repeats);
            if (index >= 0) RepeatsIndex = index;
        }

        if (defaults.TraceCount is { } traceCount)
            _editorState = _editorState with { TraceSlotCount = Math.Clamp(traceCount, 0, 8) };
        if (!preserveContent && defaults.Title is { } title) Title = title;
        if (defaults.TraceColor is { Length: > 0 } traceColor && Color.TryParse(traceColor, out _))
            TraceColor = traceColor;
        if (defaults.TraceIntensity is { Length: > 0 } traceIntensity)
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

        if (defaults.CellsPerLine is { } cellsPerLine)
        {
            var index = Array.IndexOf(CellsPerLineValues, cellsPerLine);
            if (index >= 0)
            {
                CellsPerLineIndex = index;
            }
            else
            {
                SetEditorState(_editorState with { CellsPerLine = Math.Clamp(cellsPerLine, 0, 64) },
                    nameof(CellsPerLineIndex));
            }
        }

        if (defaults.BlankCellLineCount is { } blankCellLineCount)
            BlankCellLineCount = blankCellLineCount;
        if (defaults.GroupGap is { } groupGap)
            GroupGapMm = groupGap;
        if (defaults.GridColor is { Length: > 0 } gridColor && Color.TryParse(gridColor, out _))
        {
            _editorState = _editorState with { GridColor = gridColor };
            RaisePropertyChanged(nameof(GridColorIndex));
            RaisePropertyChanged(nameof(GridColorHex));
            RequestRebuild();
        }

        if (defaults.TextColor is { Length: > 0 } textColor && Color.TryParse(textColor, out _))
        {
            _editorState = _editorState with { TextColor = textColor };
            RaisePropertyChanged(nameof(TextColorIndex));
            RaisePropertyChanged(nameof(TextColorHex));
            RequestRebuild();
        }

        if (defaults.PageSize is { Length: > 0 } pageSize)
            PageSizeIndex = pageSize.ToLowerInvariant() switch
            {
                "a4landscape" => 1,
                "a3portrait" => 2,
                "letter" => 3,
                _ => 0
            };
        if (defaults.PageMargin is { } pageMargin) PageMarginMm = pageMargin;
        ApplyPageMargins(defaults);
        if (defaults.FontFamily is { Length: > 0 } fontFamily)
            SelectedSheetFont = _fontCatalog.Find(fontFamily) ??
                                FontOption.CreateUnclassified(fontFamily.Trim());
        if (defaults.HeaderPreset is { Length: > 0 } headerPreset)
            HeaderPresetIndex = headerPreset.ToLowerInvariant() switch
            {
                "none" => 0,
                "fields" => 1,
                "titleandfields" => 2,
                "poem" => 3,
                "custom" => 4,
                _ => HeaderPresetIndex
            };
        if (defaults.HeaderText is { } headerText) HeaderTextTemplate = headerText;
        if (_editorState.BlankContentLayout)
        {
            InputText = string.Empty;
        }
        else if (!preserveContent && defaults.Text is { } text)
        {
            InputText = text;
        }

        // 旧模板用 showPoemHeader 表示诗词题头，升级后统一映射到“诗词题头”预设。
        if (!hasHeaderPreset && ShowPoemHeader)
            HeaderPresetIndex = (int)SheetHeaderPreset.Poem;

        if (preserveContent && preservedContent is { } content)
        {
            if (_editorState.BlankContentLayout)
                RestoreHeaderContent(content);
            else
                RestoreContent(content);
        }
    }

    private void RefreshTemplateChoices()
    {
        TemplateChoices = _catalog.Modules
            .Where(static module => module.Enabled)
            .ToArray();

        if (_module is null) return;
        var current = TemplateChoices.FirstOrDefault(module =>
            string.Equals(module.Id, _module.Id, StringComparison.OrdinalIgnoreCase));
        if (current is not null)
            SetCurrentModule(current);
    }

    private void ConfigureTemplateSearchDebounce()
    {
        var subscription = _templateSearchTextChanges
            .Throttle(TimeSpan.FromMilliseconds(250), TaskPoolScheduler.Default)
            .DistinctUntilChanged(StringComparer.CurrentCultureIgnoreCase)
            .Subscribe(_ => Dispatcher.UIThread.Post(ApplyTemplateFilter, DispatcherPriority.Background));
        _subscriptions.Add(subscription);
    }

    private void ResetTemplateSearch()
    {
        TemplateSearchText = string.Empty;
        ApplyTemplateFilter();
    }

    private void ApplyTemplateFilter()
    {
        var keyword = TemplateSearchText.Trim();
        var filtered = TemplateChoices
            .Where(module => ModuleSearch.Matches(module, keyword))
            .ToArray();
        FilteredTemplateChoices = filtered;

        if (_isTemplatePickerOpen &&
            _previewTemplateModule is not null &&
            !filtered.Any(module => SameModule(module, _previewTemplateModule)))
            PreviewTemplateModule = null;

        RaisePropertyChanged(nameof(TemplateFilterSummary));
    }

    private ModuleDefinition? FindTemplateChoice(ModuleDefinition? module)
    {
        if (module is null) return null;
        return TemplateChoices.FirstOrDefault(choice => SameModule(choice, module));
    }

    private bool IsCurrentModule(ModuleDefinition module)
    {
        return SameModule(_module, module);
    }

    private static bool SameModule(ModuleDefinition? left, ModuleDefinition? right)
    {
        if (left is null || right is null) return left is null && right is null;
        return string.Equals(left.Id, right.Id, StringComparison.OrdinalIgnoreCase);
    }

    private void SetCurrentModule(ModuleDefinition? module)
    {
        if ((_module is null && module is null) ||
            ReferenceEquals(_module, module))
            return;

        _module = module;
        RaisePropertyChanged(nameof(SelectedModule));
        RaisePropertyChanged(nameof(CurrentTemplateName));
    }

    private void RestoreContent(ContentSnapshot content)
    {
        InputText = content.InputText;
        RestoreHeaderContent(content);
    }

    private void RestoreHeaderContent(ContentSnapshot content)
    {
        Title = content.Title;
        Author = content.Author;
        Dynasty = content.Dynasty;
        ShowPoemHeader = content.ShowPoemHeader;
    }

    private void ApplyPageMargins(ModuleDefaults defaults)
    {
        if (defaults.PageMarginTop is null &&
            defaults.PageMarginBottom is null &&
            defaults.PageMarginLeft is null &&
            defaults.PageMarginRight is null)
            return;

        var page = _editorState.Page;
        SetEditorState(_editorState with
        {
            Page = page with
            {
                MarginTopMm = ClampPageMargin(defaults.PageMarginTop ?? page.MarginTopMm),
                MarginBottomMm = ClampPageMargin(defaults.PageMarginBottom ?? page.MarginBottomMm),
                MarginLeftMm = ClampPageMargin(defaults.PageMarginLeft ?? page.MarginLeftMm),
                MarginRightMm = ClampPageMargin(defaults.PageMarginRight ?? page.MarginRightMm)
            }
        }, nameof(PageMarginMm));
    }

    private static double ClampPageMargin(double value)
    {
        return Math.Clamp(value, 0, 80);
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
        _isContentPickerOpen = false;
        _isTemplatePickerOpen = false;
        _contentPreviewSnapshot = null;
        _templatePreviewSnapshot = null;
        _previewTemplateModule = null;
        _templateSearchText = string.Empty;
        SetCurrentModule(null);
        ApplyTemplateFilter();

        RaisePropertyChanged(string.Empty);
    }

    public bool IsNavigationTarget(NavigationContext navigationContext)
    {
        return true;
    }

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        if (_isContentPickerOpen) CloseContentPicker(commit: false);
        if (_isTemplatePickerOpen) CloseTemplatePicker(commit: false);
    }

    public void Dispose()
    {
        _subscriptions.Dispose();
        _templateSearchTextChanges.Dispose();
    }

    private sealed record EditorPreviewSnapshot(
        SheetEditorState State,
        FontOption? SelectedSheetFont,
        ModuleDefinition? Module,
        int PageIndex,
        bool IsDirty,
        string StatusMessage);

    private sealed record ContentSnapshot(
        string InputText,
        string Title,
        string Author,
        string Dynasty,
        bool ShowPoemHeader)
    {
        public static ContentSnapshot Capture(SheetEditorState state)
        {
            return new ContentSnapshot(
                state.InputText,
                state.Title,
                state.Author ?? string.Empty,
                state.Dynasty ?? string.Empty,
                state.ShowPoemHeader);
        }
    }
}
