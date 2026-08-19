using System.Text.Json;
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
    private readonly IRegionNavigationJournal _journal;

    private string _inputText = "床前明月光，疑是地上霜。举头望明月，低头思故乡。";
    private string _title = string.Empty;
    private int _gridKindIndex;
    private int _practiceModeIndex;
    private int _repeatsIndex = 3;
    private int _traceSlotCount = 2;
    private bool _groupByWord;
    private bool _showPinyin;
    private bool _pinyinOnly;
    private int _textEntryIndex = -1;
    private double _zoom = 1.0;
    private int _pageIndex;
    private CharacterSheetSpec _spec = new();
    private IReadOnlyList<SheetPage> _pages = Array.Empty<SheetPage>();
    private ModuleDefinition? _module;

    public SheetEditorViewModel(
        ModuleCatalog catalog,
        TextCatalog textCatalog,
        PinyinCatalog pinyinCatalog,
        IRegionNavigationJournal journal)
    {
        _catalog = catalog;
        _textCatalog = textCatalog;
        _pinyinCatalog = pinyinCatalog;
        _journal = journal;

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

    public int[] RepeatsChoices { get; } = Enumerable.Range(2, 8).ToArray();

    /// <summary>文本库条目（下拉选择后填充 InputText）。</summary>
    public IReadOnlyList<TextEntry> TextEntries => _textCatalog.Entries;

    public int TextEntryIndex
    {
        get => _textEntryIndex;
        set
        {
            if (SetProperty(ref _textEntryIndex, value) && value >= 0 && value < TextEntries.Count)
            {
                InputText = TextEntries[value].Body;
            }
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
            if (SetProperty(ref _practiceModeIndex, value)) Rebuild();
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
        var spec = new CharacterSheetSpec
        {
            Text = InputText,
            Title = string.IsNullOrWhiteSpace(Title) ? null : Title.Trim(),
            Grid = (GridKind)GridKindIndex,
            Mode = (PracticeMode)PracticeModeIndex,
            RepeatsPerChar = RepeatsChoices[Math.Clamp(RepeatsIndex, 0, RepeatsChoices.Length - 1)],
            TraceSlotCount = _traceSlotCount,
            GroupByWord = _groupByWord,
            ShowPinyin = _showPinyin,
            PinyinOnly = _pinyinOnly,
            PinyinByGlyph = _showPinyin ? _pinyinCatalog.PinyinByGlyph : null
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
                        _ => GridKindIndex
                    };
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
                case "text" when property.Value.GetString() is { } text:
                    InputText = text;
                    break;
            }
    }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        var module = _catalog.Find(navigationContext.Parameters.GetValue<string?>("moduleId"));
        if (module is not null) ApplyModuleDefaults(module);

        Rebuild();
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
