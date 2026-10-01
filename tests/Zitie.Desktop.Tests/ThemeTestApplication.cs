using System;
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

/// <summary>
/// Avalonia Application 是进程级共享的：每个测试类各建一个 Headless 会话会导致
/// 后续类在别的线程访问首个会话创建的 Compositor（"different thread owns it"）。
/// 因此全部测试类共享同一个静态会话，生命周期跟随进程，Dispose 不做清理。
/// </summary>
public sealed class AvaloniaHeadlessFixture : IAsyncLifetime
{
    private static readonly Lazy<HeadlessUnitTestSession> SharedSession = new(
        () => HeadlessUnitTestSession.StartNew(
            typeof(ThemeTestAppBuilder),
            AvaloniaTestIsolationLevel.PerAssembly),
        LazyThreadSafetyMode.ExecutionAndPublication);

    public AvaloniaHeadlessFixture()
    {
        Session = SharedSession.Value;
    }

    public HeadlessUnitTestSession Session { get; }

    public ValueTask InitializeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        // 共享会话由进程退出统一回收，避免影响后续测试类。
        return ValueTask.CompletedTask;
    }
}
