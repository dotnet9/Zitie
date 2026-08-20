using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Zitie.Desktop.Services;

namespace Zitie.Desktop.ViewModels;

public class ModuleGalleryViewModel : BindableBase
{
    public const double MinimumGalleryZoom = 0.8;
    public const double MaximumGalleryZoom = 1.6;
    public const double DefaultGalleryZoom = 1;

    private readonly IRegionManager _regionManager;
    private readonly ModuleCatalog _catalog;
    private readonly ISystemDialogs _dialogs;
    private readonly DelegateCommand<ModuleDefinition> _openCommand;
    private string _searchText = string.Empty;
    private string _selectedCategory = "全部";
    private double _galleryZoom = DefaultGalleryZoom;

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
        RefreshCommand = new DelegateCommand(Refresh);
        RebuildModuleCards();
    }

    public IReadOnlyList<ModuleCardViewModel> Modules { get; private set; } = Array.Empty<ModuleCardViewModel>();

    public string TemplateDirectory { get; }

    public DelegateCommand OpenTemplateDirectoryCommand { get; }

    public DelegateCommand RefreshCommand { get; }

    public IReadOnlyList<CategoryChoiceViewModel> CategoryChoices { get; private set; } =
        Array.Empty<CategoryChoiceViewModel>();

    public IReadOnlyList<ModuleCardViewModel> FilteredModules { get; private set; } = Array.Empty<ModuleCardViewModel>();

    public int FilteredModuleCount => FilteredModules.Count;

    public bool IsEmpty => FilteredModules.Count == 0;

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
            if (SetProperty(ref _searchText, value ?? string.Empty))
                ApplyFilter();
        }
    }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, string.IsNullOrWhiteSpace(value) ? "全部" : value))
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

    private void Refresh()
    {
        _catalog.Reload();
        RebuildModuleCards();
        ZitieLogging.Info("模板页已刷新");
    }

    private void RebuildModuleCards()
    {
        Modules = _catalog.Modules
            .Select(module => new ModuleCardViewModel(module, _openCommand))
            .ToList();
        foreach (var module in Modules)
            module.CardZoom = GalleryZoom;
        var categoryNames = new[] { "全部" }
            .Concat(Modules
                .Select(module => module.CategoryText)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(category => category, StringComparer.CurrentCultureIgnoreCase))
            .ToArray();
        CategoryChoices = categoryNames
            .Select(category => new CategoryChoiceViewModel(category,
                string.Equals(category, SelectedCategory, StringComparison.CurrentCultureIgnoreCase),
                SelectCategory))
            .ToList();

        RaisePropertyChanged(nameof(Modules));
        RaisePropertyChanged(nameof(CategoryChoices));
        ApplyFilter();
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
        if (!string.IsNullOrWhiteSpace(category)) SelectedCategory = category;
    }

    private void ApplyFilter()
    {
        var keyword = SearchText.Trim();
        var category = SelectedCategory;

        FilteredModules = Modules
            .Where(module => string.Equals(category, "全部", StringComparison.CurrentCultureIgnoreCase) ||
                             string.Equals(module.CategoryText, category, StringComparison.CurrentCultureIgnoreCase))
            .Where(module => keyword.Length == 0 ||
                             module.Module.Name.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                             module.Module.Description.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                             module.CategoryText.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
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
}
