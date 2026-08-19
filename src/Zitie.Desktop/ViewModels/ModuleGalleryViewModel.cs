using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Zitie.Desktop.Services;

namespace Zitie.Desktop.ViewModels;

public class ModuleGalleryViewModel : BindableBase
{
    private readonly IRegionManager _regionManager;

    public ModuleGalleryViewModel(IRegionManager regionManager, ModuleCatalog catalog)
    {
        _regionManager = regionManager;

        var openCommand = new DelegateCommand<ModuleDefinition>(
            OpenModule,
            module => module is { Enabled: true });

        Modules = catalog.Modules
            .Select(module => new ModuleCardViewModel(module, openCommand))
            .ToList();
    }

    public IReadOnlyList<ModuleCardViewModel> Modules { get; }

    private void OpenModule(ModuleDefinition? module)
    {
        if (module is null || !module.Enabled) return;

        ZitieLogging.Info($"打开模块：{module.Name}（{module.Id}）");
        _regionManager.RequestNavigate("MainRegion", "SheetEditor",
            new NavigationParameters { { "moduleId", module.Id } });
    }
}
