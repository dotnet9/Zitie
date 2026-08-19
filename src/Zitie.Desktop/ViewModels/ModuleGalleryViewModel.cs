using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Zitie.Desktop.Services;

namespace Zitie.Desktop.ViewModels;

public class ModuleGalleryViewModel : BindableBase
{
    private readonly IRegionManager _regionManager;
    private string _searchText = string.Empty;
    private string _selectedCategory = "全部";

    public ModuleGalleryViewModel(IRegionManager regionManager, ModuleCatalog catalog)
    {
        _regionManager = regionManager;

        var openCommand = new DelegateCommand<ModuleDefinition>(
            OpenModule,
            module => module is { Enabled: true });

        Modules = catalog.Modules
            .Select(module => new ModuleCardViewModel(module, openCommand))
            .ToList();
        CategoryChoices = ["全部", .. Modules
            .Select(module => module.CategoryText)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(category => category, StringComparer.CurrentCultureIgnoreCase)];
        FilteredModules = Modules;
    }

    public IReadOnlyList<ModuleCardViewModel> Modules { get; }

    public IReadOnlyList<string> CategoryChoices { get; }

    public IReadOnlyList<ModuleCardViewModel> FilteredModules { get; private set; }

    public int FilteredModuleCount => FilteredModules.Count;

    public bool IsEmpty => FilteredModules.Count == 0;

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

        RaisePropertyChanged(nameof(FilteredModules));
        RaisePropertyChanged(nameof(FilteredModuleCount));
        RaisePropertyChanged(nameof(IsEmpty));
    }
}
