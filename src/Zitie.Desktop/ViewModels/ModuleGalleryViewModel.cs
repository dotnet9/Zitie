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
    private string _loadingMessage = "正在加载模板...";
    private double _galleryZoom = DefaultGalleryZoom;
    private bool _isLoading = true;

    public ModuleGalleryViewModel(
        IRegionManager regionManager,
        ModuleCatalog catalog,
        ISystemDialogs dialogs)
    {
        _regionManager = regionManager;
        _catalog = catalog;
        _dialogs = dialogs;
        TemplateDirectory = catalog.ImageDirectory;
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

    public int FilteredModuleCount => FilteredModules.Count;

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (!SetProperty(ref _isLoading, value)) return;
            RefreshCommand.RaiseCanExecuteChanged();
            RaisePropertyChanged(nameof(IsEmpty));
        }
    }

    public string LoadingMessage
    {
        get => _loadingMessage;
        private set => SetProperty(ref _loadingMessage, value);
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
        Dispatcher.UIThread.Post(() => _ = LoadModuleCardsAsync(reloadCatalog: false), DispatcherPriority.Background);
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
        var revision = Interlocked.Increment(ref _loadRevision);
        LoadingMessage = reloadCatalog ? "正在刷新模板..." : "正在加载模板...";
        IsLoading = true;

        try
        {
            var cards = await Task.Run(() =>
            {
                if (reloadCatalog) _catalog.Reload();
                return _catalog.Modules
                    .Select(module => new ModuleCardViewModel(module, _openCommand))
                    .ToList();
            });

            if (revision != _loadRevision) return;

            ApplyModuleCards(cards);
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
            if (revision == _loadRevision)
                IsLoading = false;
        }
    }

    private void ApplyModuleCards(IReadOnlyList<ModuleCardViewModel> modules)
    {
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
        ApplyFilter();
    }

    private void OpenTemplateDirectory()
    {
        try
        {
            Directory.CreateDirectory(TemplateDirectory);
            _dialogs.OpenFolder(TemplateDirectory);
            ZitieLogging.Info($"已打开示例图目录：{TemplateDirectory}");
        }
        catch (Exception exception)
        {
            ZitieLogging.Warn($"打开示例图目录失败：{TemplateDirectory}", exception);
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
        RaisePropertyChanged(nameof(FilteredModuleCount));
        RaisePropertyChanged(nameof(IsEmpty));
    }

    public void AdjustZoom(double delta)
    {
        GalleryZoom += delta;
    }

    public void Dispose()
    {
        _subscriptions.Dispose();
        _searchTextChanges.Dispose();
    }
}
