using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Regions;
using Zitie.Desktop.Services;
using Zitie.Desktop.ViewModels;
using Zitie.Desktop.Views;

namespace Zitie.Desktop;

public partial class App : PrismApplication
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        base.Initialize();
    }

    protected override AvaloniaObject CreateShell()
    {
        return Container.Resolve<MainWindow>();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        ZitieLogging.Initialize();
        containerRegistry.RegisterSingleton<ModuleCatalog>();
        containerRegistry.RegisterSingleton<TextCatalog>();
        containerRegistry.RegisterSingleton<PinyinCatalog>();
        containerRegistry.RegisterSingleton<FontCatalog>();
        // fork 把导航历史注册为瞬时实例，导致注入到 ViewModel 的 journal 与区域导航服务
        // 各自持有一份空历史，返回按钮点击无效。改为单例共享同一历史。
        containerRegistry.RegisterSingleton<IRegionNavigationJournal, RegionNavigationJournal>();
        containerRegistry.RegisterForNavigation<ModuleGalleryView, ModuleGalleryViewModel>("ModuleGallery");
        containerRegistry.RegisterForNavigation<SheetEditorView, SheetEditorViewModel>("SheetEditor");
    }

    protected override void OnInitialized()
    {
        // Prism 的区域由 DelayedRegionCreationBehavior 在窗口视觉树附加后才注册，
        // 此时 RequestNavigate 会因区域不存在而静默返回 False。因此把首次导航推迟到窗口 Opened。
        if (MainWindow is Window shell)
        {
            shell.Opened += (_, _) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    var regionManager = Container.Resolve<IRegionManager>();
                    ZitieLogging.Info($"已注册区域：{(regionManager.Regions.Count() == 0 ? "(无)" : string.Join(",", regionManager.Regions.Select(r => r.Name)))}");
                    regionManager.RequestNavigate("MainRegion", "ModuleGallery");
                });
            };
        }
    }
}
