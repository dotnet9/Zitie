using Avalonia;
using Avalonia.Markup.Xaml;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Regions;
using Zitie.Desktop.Services;
using Zitie.Desktop.ViewModels;
using Zitie.Desktop.Views;
using CodeWF.Avalonia.Controls.Themes;
using CodeWF.Toolkit.Core.UpdateChecking;
using CodeWF.Toolkit.Lang;
using System.Globalization;

namespace Zitie.Desktop;

public partial class App : PrismApplication
{
    private readonly DeviceSettingsService? _settings;
    public App() { }
    public App(DeviceSettingsService settings) => _settings = settings;
    public static StudioThemeController ThemeController { get; private set; } = null!;

    public override void Initialize()
    {
        AppText.Initialize();
        AvaloniaXamlLoader.Load(this);
        ThemeController = new StudioThemeController(this);
        I18nManager.Instance.CultureChanged += (_, _) =>
        {
            foreach (var theme in Styles.OfType<Semi.Avalonia.SemiTheme>()) theme.Locale = I18nManager.Instance.Culture;
        };
        base.Initialize();
    }

    protected override AvaloniaObject CreateShell()
    {
        return Container.Resolve<MainWindow>();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        ZitieLogging.Initialize();
        var settings = _settings ?? new DeviceSettingsService();
        containerRegistry.RegisterInstance(settings);
        ThemeController.Mode = Enum.TryParse<StudioThemeMode>(settings.ThemeMode, out var mode) ? mode : StudioThemeMode.System;
        ThemeController.Palette = Enum.TryParse<StudioPalette>(settings.Palette, out var palette) ? palette : StudioPalette.DotNet;
        I18nManager.Instance.Culture = CultureInfo.GetCultureInfo(settings.Language);
        containerRegistry.RegisterInstance<IUpdateService>(new UpdateService(new UpdateServiceOptions
        {
            Owner = "dotnet9", Repository = "Zitie", ProductName = "Zitie", PreferInstaller = true
        }));
        containerRegistry.RegisterSingleton<AboutViewModel>();
        containerRegistry.RegisterInstance(new ModuleCatalog(loadImmediately: false));
        containerRegistry.RegisterSingleton<TextCatalog>();
        containerRegistry.RegisterSingleton<TextbookCatalog>();
        containerRegistry.RegisterSingleton<PinyinCatalog>();
        containerRegistry.RegisterSingleton<StrokeOrderCatalog>();
        containerRegistry.RegisterSingleton<FontCatalog>();
        containerRegistry.RegisterSingleton<ISystemDialogs, SystemDialogsService>();
        // fork 把导航历史注册为瞬时实例，导致注入到 ViewModel 的 journal 与区域导航服务
        // 各自持有一份空历史，返回按钮点击无效。改为单例共享同一历史。
        containerRegistry.RegisterSingleton<IRegionNavigationJournal, RegionNavigationJournal>();
        containerRegistry.RegisterForNavigation<ModuleGalleryView, ModuleGalleryViewModel>("ModuleGallery");
        containerRegistry.RegisterForNavigation<SheetEditorView, SheetEditorViewModel>("SheetEditor");
    }

}
