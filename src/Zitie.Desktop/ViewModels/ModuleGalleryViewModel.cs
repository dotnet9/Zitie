using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Avalonia.Threading;
using Zitie.Desktop.Services;

namespace Zitie.Desktop.ViewModels;

public class ModuleGalleryViewModel : BindableBase, IDisposable
{
    private static readonly string[] PreferredCategories =
    [
        "汉字", "拼音", "数字", "英文", "有笔顺", "组词", "书法", "生字", "脱格", "文章", "试卷", "测试卡", "名字", "封面"
    ];

    public const double MinimumGalleryZoom = 0.8;
    public const double MaximumGalleryZoom = 1.6;
    public const double DefaultGalleryZoom = 1;

    private readonly IRegionManager _regionManager;
    private readonly ModuleCatalog _catalog;
    private readonly ISystemDialogs _dialogs;
    private readonly DelegateCommand<ModuleDefinition> _openCommand;
    private readonly Subject<string> _searchTextChanges = new();
    private readonly CompositeDisposable _subscriptions = new();
    private int _loadRevision;
    private string _searchText = string.Empty;
    private string _selectedCategory = string.Empty;
    private double _galleryZoom = DefaultGalleryZoom;
    private double _galleryAvailableWidth = 1100;
    private int _galleryColumnCount = 4;
    private bool _isLoading = true;
    private bool _isDisposed;

    public ModuleGalleryViewModel(
        IRegionManager regionManager,
        ModuleCatalog catalog,
        ISystemDialogs dialogs)
    {
        _regionManager = regionManager;
        _catalog = catalog;
        _dialogs = dialogs;
        TemplateDirectory = catalog.UserDirectory;
        OpenTemplateDirectoryCommand = new DelegateCommand(OpenTemplateDirectory);

        _openCommand = new DelegateCommand<ModuleDefinition>(
            OpenModule,
            module => module is { Enabled: true });
        RefreshCommand = new DelegateCommand(
            () => _ = LoadModuleCardsAsync(reloadCatalog: true),
            () => !IsLoading);
        ConfigureSearchDebounce();
        QueueInitialLoad();
    }

    public IReadOnlyList<ModuleCardViewModel> Modules { get; private set; } = Array.Empty<ModuleCardViewModel>();

    public string TemplateDirectory { get; }

    public DelegateCommand OpenTemplateDirectoryCommand { get; }

    public DelegateCommand RefreshCommand { get; }

    public IReadOnlyList<CategoryChoiceViewModel> CategoryChoices { get; private set; } =
        Array.Empty<CategoryChoiceViewModel>();

    public IReadOnlyList<ModuleCardViewModel> FilteredModules { get; private set; } = Array.Empty<ModuleCardViewModel>();

    public IReadOnlyList<ModuleCardRowViewModel> FilteredModuleRows { get; private set; } =
        Array.Empty<ModuleCardRowViewModel>();

    public IReadOnlyList<int> LoadingPlaceholders { get; } = Enumerable.Range(0, 6).ToArray();

    public int FilteredModuleCount => FilteredModules.Count;

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (!SetProperty(ref _isLoading, value)) return;
            RefreshCommand.RaiseCanExecuteChanged();
            RaisePropertyChanged(nameof(IsEmpty));
            RaisePropertyChanged(nameof(IsInitialLoading));
            RaisePropertyChanged(nameof(GalleryContentOpacity));
        }
    }

    public string CatalogSummary
    {
        get
        {
            if (IsLoading && Modules.Count == 0) return "正在加载模板...";

            var counts = Modules
                .SelectMany(module => module.Module.Categories)
                .GroupBy(category => category, StringComparer.CurrentCultureIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.CurrentCultureIgnoreCase);
            var parts = new List<string> { $"{Modules.Count} 个模板" };
            parts.AddRange(PreferredCategories
                .Where(counts.ContainsKey)
                .Select(category => $"{category} {counts[category]}"));
            return string.Join(" · ", parts);
        }
    }

    public bool IsEmpty => !IsLoading && FilteredModules.Count == 0;

    public bool IsInitialLoading => IsLoading && Modules.Count == 0;

    public double GalleryContentOpacity => IsInitialLoading ? 0 : 1;

    public double MinimumZoom => MinimumGalleryZoom;

    public double MaximumZoom => MaximumGalleryZoom;

    public double GalleryZoom
    {
        get => _galleryZoom;
        set
        {
            var zoom = Math.Round(Math.Clamp(value, MinimumGalleryZoom, MaximumGalleryZoom), 2,
                MidpointRounding.AwayFromZero);
            if (!SetProperty(ref _galleryZoom, zoom)) return;
            foreach (var module in Modules)
                module.CardZoom = zoom;
            RebuildRows();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (SetProperty(ref _searchText, normalized))
                _searchTextChanges.OnNext(normalized);
        }
    }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value?.Trim() ?? string.Empty))
                ApplyFilter();
        }
    }

    private void OpenModule(ModuleDefinition? module)
    {
        if (module is null || !module.Enabled) return;

        ZitieLogging.Info($"打开模块：{module.Name}（{module.Id}）");
        _regionManager.RequestNavigate("MainRegion", "SheetEditor",
            new NavigationParameters { { "moduleId", module.Id } });
    }

    private void QueueInitialLoad()
    {
        Dispatcher.UIThread.Post(
            () =>
            {
                if (!_isDisposed) _ = LoadModuleCardsAsync(reloadCatalog: false);
            },
            DispatcherPriority.Background);
    }

    private void ConfigureSearchDebounce()
    {
        var subscription = _searchTextChanges
            .Throttle(TimeSpan.FromMilliseconds(250), TaskPoolScheduler.Default)
            .DistinctUntilChanged(StringComparer.CurrentCultureIgnoreCase)
            .Subscribe(_ => Dispatcher.UIThread.Post(ApplyFilter, DispatcherPriority.Background));
        _subscriptions.Add(subscription);
    }

    private async Task LoadModuleCardsAsync(bool reloadCatalog)
    {
        if (_isDisposed) return;

        var revision = Interlocked.Increment(ref _loadRevision);
        var stopwatch = Stopwatch.StartNew();
        IsLoading = true;

        try
        {
            var cards = await Task.Run(() =>
            {
                if (reloadCatalog)
                    _catalog.Reload();
                else
                    _catalog.EnsureLoaded();
                return _catalog.Modules
                    .Select(module => new ModuleCardViewModel(module, _openCommand))
                    .ToList();
            });

            if (revision != _loadRevision || _isDisposed)
            {
                DisposeCards(cards);
                return;
            }

            ApplyModuleCards(cards);
            ZitieLogging.Info($"模板卡片索引准备完成：{cards.Count} 个，耗时 {stopwatch.ElapsedMilliseconds} ms");
            if (reloadCatalog)
            {
                ZitieLogging.Info("模板页已刷新");
            }
        }
        catch (Exception exception)
        {
            ZitieLogging.Warn("模板卡片加载失败", exception);
        }
        finally
        {
            if (revision == _loadRevision && !_isDisposed)
                IsLoading = false;
        }
    }

    private void ApplyModuleCards(IReadOnlyList<ModuleCardViewModel> modules)
    {
        var previousModules = Modules;
        Modules = modules;
        foreach (var module in Modules)
            module.CardZoom = GalleryZoom;
        var categories = Modules
            .SelectMany(module => module.Module.Categories)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        var categoryNames = PreferredCategories
            .Where(category => categories.Contains(category, StringComparer.CurrentCultureIgnoreCase))
            .Concat(categories
                .Where(category => !PreferredCategories.Contains(category, StringComparer.CurrentCultureIgnoreCase))
                .OrderBy(category => category, StringComparer.CurrentCultureIgnoreCase))
            .ToArray();
        CategoryChoices = categoryNames
            .Select(category => new CategoryChoiceViewModel(category,
                string.Equals(category, SelectedCategory, StringComparison.CurrentCultureIgnoreCase),
                SelectCategory))
            .ToList();

        RaisePropertyChanged(nameof(Modules));
        RaisePropertyChanged(nameof(CategoryChoices));
        RaisePropertyChanged(nameof(CatalogSummary));
        RaisePropertyChanged(nameof(IsInitialLoading));
        RaisePropertyChanged(nameof(GalleryContentOpacity));
        ApplyFilter();
        DisposeCards(previousModules);
    }

    private void OpenTemplateDirectory()
    {
        try
        {
            Directory.CreateDirectory(TemplateDirectory);
            _dialogs.OpenFolder(TemplateDirectory);
            ZitieLogging.Info($"已打开用户模板目录：{TemplateDirectory}");
        }
        catch (Exception exception)
        {
            ZitieLogging.Warn($"打开用户模板目录失败：{TemplateDirectory}", exception);
        }
    }

    private void SelectCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category)) return;

        SelectedCategory = string.Equals(category, SelectedCategory, StringComparison.CurrentCultureIgnoreCase)
            ? string.Empty
            : category;
    }

    private void ApplyFilter()
    {
        if (_isDisposed) return;

        var keyword = SearchText.Trim();
        var category = SelectedCategory;

        FilteredModules = Modules
            .Where(module => category.Length == 0 ||
                             module.Module.Categories.Contains(category, StringComparer.CurrentCultureIgnoreCase))
            .Where(module => ModuleSearch.Matches(module.Module, keyword))
            .ToList();

        foreach (var choice in CategoryChoices)
            choice.IsSelected = string.Equals(choice.Name, SelectedCategory,
                StringComparison.CurrentCultureIgnoreCase);

        RaisePropertyChanged(nameof(FilteredModules));
        RebuildRows();
        RaisePropertyChanged(nameof(FilteredModuleCount));
        RaisePropertyChanged(nameof(IsEmpty));
    }

    public void UpdateGalleryWidth(double availableWidth)
    {
        if (!double.IsFinite(availableWidth) || availableWidth <= 0) return;

        _galleryAvailableWidth = availableWidth;
        var columnCount = CalculateGalleryColumnCount(availableWidth, GalleryZoom);
        if (_galleryColumnCount == columnCount) return;

        _galleryColumnCount = columnCount;
        RebuildRows();
    }

    internal static int CalculateGalleryColumnCount(double availableWidth, double zoom)
    {
        if (!double.IsFinite(availableWidth) || availableWidth <= 0) return 1;

        var normalizedZoom = Math.Clamp(zoom, MinimumGalleryZoom, MaximumGalleryZoom);
        var slotWidth = ModuleCardViewModel.BaseCardWidth * normalizedZoom +
                        ModuleCardViewModel.CardHorizontalSpacing;
        return Math.Clamp(
            (int)Math.Floor((availableWidth + ModuleCardViewModel.CardHorizontalSpacing) / slotWidth),
            1,
            12);
    }

    private void RebuildRows()
    {
        var columnCount = CalculateGalleryColumnCount(_galleryAvailableWidth, GalleryZoom);
        _galleryColumnCount = columnCount;
        FilteredModuleRows = FilteredModules
            .Chunk(columnCount)
            .Select(static cards => new ModuleCardRowViewModel(cards))
            .ToList();
        RaisePropertyChanged(nameof(FilteredModuleRows));
    }

    public void AdjustZoom(double delta)
    {
        GalleryZoom += delta;
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;
        Interlocked.Increment(ref _loadRevision);
        _subscriptions.Dispose();
        _searchTextChanges.Dispose();
        DisposeCards(Modules);
        Modules = Array.Empty<ModuleCardViewModel>();
        FilteredModules = Array.Empty<ModuleCardViewModel>();
        FilteredModuleRows = Array.Empty<ModuleCardRowViewModel>();
    }

    private static void DisposeCards(IEnumerable<ModuleCardViewModel> modules)
    {
        foreach (var module in modules)
            module.Dispose();
    }
}

public sealed record ModuleCardRowViewModel(IReadOnlyList<ModuleCardViewModel> Modules);
