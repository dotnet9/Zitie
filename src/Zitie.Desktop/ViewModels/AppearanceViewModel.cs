using System.Globalization;
using Avalonia.Media;
using CodeWF.Avalonia.Controls.Themes;
using CodeWF.Toolkit.Lang;
using Zitie.Desktop.Services;
using System.Windows.Input;
using Prism.Commands;

namespace Zitie.Desktop.ViewModels;

/// <summary>外观选项保持稳定身份，显示文案随全局语言变化。</summary>
public sealed class AppearanceChoice(string key, string resourceKey, IBrush swatch) : LocalizedBindableBase
{
    public string Key { get; } = key;
    public string Text => AppText.Get(resourceKey);
    public IBrush Swatch { get; } = swatch;
    public ICommand? SelectCommand { get; set; }
}

/// <summary>标题栏外观选项与设备偏好；三态明暗模式和配色分别持久化。</summary>
public sealed class AppearanceViewModel : LocalizedBindableBase
{
    private readonly DeviceSettingsService settings;
    public AppearanceViewModel(DeviceSettingsService settings)
    {
        this.settings = settings;
        foreach (var language in Languages) language.SelectCommand = new DelegateCommand(() => SelectedLanguage = language);
    }
    public StudioThemeMode ThemeMode
    {
        get => Enum.Parse<StudioThemeMode>(SelectedMode!.Key);
        set => SelectedMode = Modes.Single(option => option.Key == value.ToString());
    }
    public IReadOnlyList<AppearanceChoice> Modes { get; } =
    [
        new("System", Language.SystemMode, Brushes.Transparent),
        new("Light", Language.LightMode, Brushes.Transparent),
        new("Dark", Language.DarkMode, Brushes.Transparent)
    ];
    public IReadOnlyList<AppearanceChoice> Languages { get; } =
    [
        new("zh-CN", Language.Chinese, Brushes.Transparent),
        new("en-US", Language.English, Brushes.Transparent)
    ];
    public IReadOnlyList<AppearanceChoice> Palettes { get; } = Enum.GetValues<StudioPalette>().Select(value =>
        new AppearanceChoice(value.ToString(), value switch
        {
            StudioPalette.Default => Language.DefaultPalette, StudioPalette.Desert => Language.DesertPalette,
            StudioPalette.Aquatic => Language.AquaticPalette, StudioPalette.Dusk => Language.DuskPalette,
            StudioPalette.NightSky => Language.NightSkyPalette, StudioPalette.DotNet => Language.DotNetPalette,
            StudioPalette.Aurora => Language.AuroraPalette, _ => Language.EmberPalette
        }, Swatch(value))).ToArray();

    private static IBrush Swatch(StudioPalette value)
    {
        var application = global::Avalonia.Application.Current!;
        return application.TryGetResource("StudioSwatch" + value, application.ActualThemeVariant, out var brush) ? brush as IBrush ?? Brushes.Transparent : Brushes.Transparent;
    }

    public AppearanceChoice? SelectedMode
    {
        get => Modes.FirstOrDefault(option => option.Key == settings.ThemeMode) ?? Modes[0];
        set
        {
            if (value is null || value.Key == settings.ThemeMode) return;
            settings.ThemeMode = value.Key; App.ThemeController.Mode = Enum.Parse<StudioThemeMode>(value.Key); RaisePropertyChanged();
            RaisePropertyChanged(nameof(ThemeMode));
        }
    }
    public AppearanceChoice? SelectedPalette
    {
        get => Palettes.FirstOrDefault(option => option.Key == settings.Palette) ?? Palettes.Single(option => option.Key == "DotNet");
        set
        {
            if (value is null || value.Key == settings.Palette) return;
            settings.Palette = value.Key; App.ThemeController.Palette = Enum.Parse<StudioPalette>(value.Key); RaisePropertyChanged();
        }
    }
    public AppearanceChoice? SelectedLanguage
    {
        get => Languages.Single(option => option.Key == settings.Language);
        set
        {
            if (value is null || value.Key == settings.Language) return;
            settings.Language = value.Key; I18nManager.Instance.Culture = CultureInfo.GetCultureInfo(value.Key); RaisePropertyChanged();
        }
    }
}
