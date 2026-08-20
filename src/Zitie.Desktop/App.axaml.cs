using Avalonia;
using Avalonia.Markup.Xaml;
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
        containerRegistry.RegisterSingleton<DeviceSettingsService>();
        containerRegistry.RegisterSingleton<ModuleCatalog>();
        containerRegistry.RegisterSingleton<TextCatalog>();
        containerRegistry.RegisterSingleton<TextbookCatalog>();
        containerRegistry.RegisterSingleton<PinyinCatalog>();
        containerRegistry.RegisterSingleton<FontCatalog>();
        containerRegistry.RegisterSingleton<ISystemDialogs, SystemDialogsService>();
        // fork 把导航历史注册为瞬时实例，导致注入到 ViewModel 的 journal 与区域导航服务
        // 各自持有一份空历史，返回按钮点击无效。改为单例共享同一历史。
        containerRegistry.RegisterSingleton<IRegionNavigationJournal, RegionNavigationJournal>();
        containerRegistry.RegisterForNavigation<ModuleGalleryView, ModuleGalleryViewModel>("ModuleGallery");
        containerRegistry.RegisterForNavigation<SheetEditorView, SheetEditorViewModel>("SheetEditor");
    }

}
