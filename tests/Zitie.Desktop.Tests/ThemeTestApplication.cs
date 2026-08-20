using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Xunit;

namespace Zitie.Desktop.Tests;

public sealed class ThemeTestApplication : Application
{
    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Light;
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

public sealed class AvaloniaHeadlessFixture : IAsyncLifetime
{
    public AvaloniaHeadlessFixture()
    {
        Session = HeadlessUnitTestSession.StartNew(
            typeof(ThemeTestAppBuilder),
            AvaloniaTestIsolationLevel.PerAssembly);
    }

    public HeadlessUnitTestSession Session { get; }

    public ValueTask InitializeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await Task.Run(() => Session.DisposeAsync().AsTask()).ConfigureAwait(false);
    }
}
