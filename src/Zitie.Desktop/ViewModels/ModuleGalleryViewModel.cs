using System.Diagnostics;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Zitie.Desktop.Services;

namespace Zitie.Desktop.ViewModels;

public class ModuleGalleryViewModel : BindableBase
{
    private readonly IRegionManager _regionManager;
    private readonly ModuleCatalog _catalog;
    private readonly DelegateCommand<ModuleDefinition> _openCommand;
    private string _searchText = string.Empty;
    private string _selectedCategory = "全部";

    public ModuleGalleryViewModel(IRegionManager regionManager, ModuleCatalog catalog)
    {
        _regionManager = regionManager;
        _catalog = catalog;
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

    public IReadOnlyList<string> CategoryChoices { get; private set; } = ["全部"];

    public IReadOnlyList<ModuleCardViewModel> FilteredModules { get; private set; } = Array.Empty<ModuleCardViewModel>();

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
        CategoryChoices = ["全部", .. Modules
            .Select(module => module.CategoryText)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(category => category, StringComparer.CurrentCultureIgnoreCase)];

        RaisePropertyChanged(nameof(Modules));
        RaisePropertyChanged(nameof(CategoryChoices));
        ApplyFilter();
    }

    private void OpenTemplateDirectory()
    {
        var directory = TemplateDirectory;
        try
        {
            Directory.CreateDirectory(directory);
            var startInfo = OperatingSystem.IsWindows()
                ? new ProcessStartInfo(directory) { UseShellExecute = true }
                : OperatingSystem.IsMacOS()
                    ? new ProcessStartInfo("open", $"\"{directory}\"")
                    : new ProcessStartInfo("xdg-open", $"\"{directory}\"");

            Process.Start(startInfo);
            ZitieLogging.Info($"已打开用户模板目录：{directory}");
        }
        catch (Exception exception)
        {
            ZitieLogging.Warn($"打开用户模板目录失败：{directory}", exception);
        }
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
