using System;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Xunit;
using CodeWF.Avalonia.Controls.Themes;
using Zitie.Desktop.I18n;

namespace Zitie.Desktop.Tests;

public sealed class ThemeTestApplication : Application
{
    public override void Initialize()
    {
        AppText.Initialize();
        Styles.Add(new Semi.Avalonia.SemiTheme());
        Styles.Add(new ACSemiTheme());
        Styles.Add(new StudioTheme());
        RequestedThemeVariant = StudioTheme.DotNetLight;
        Styles.Add(new StyleInclude(new Uri("avares://Zitie.Avalonia/"))
        {
            Source = new Uri("avares://Zitie.Avalonia/Themes/Index.axaml")
        });
        Styles.Add(new StyleInclude(new Uri("avares://Zitie.Desktop/"))
        {
            Source = new Uri("avares://Zitie.Desktop/Themes/Index.axaml")
        });
    }
}

public static class ThemeTestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder
        .Configure<ThemeTestApplication>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

