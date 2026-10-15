using System;
using Avalonia;
using CodeWF.Toolkit.Core.UpdateChecking;
using System.Globalization;

namespace Zitie.Desktop;

internal static class Program
{
    // 设计器与正式程序共用同一个构建入口。
    [STAThread]
    public static int Main(string[] args)
    {
        if (Array.Exists(args, argument => argument == "--selfcheck"))
        {
            AppText.Initialize();
            string? previous = null;
            foreach (var culture in new[] { "en-US", "zh-CN" })
            {
                CodeWF.Toolkit.Lang.I18nManager.Instance.Culture = CultureInfo.GetCultureInfo(culture);
                var translated = AppText.Get(Language.About);
                if (string.IsNullOrWhiteSpace(translated) || translated == previous || translated.StartsWith("Zitie.Text.", StringComparison.Ordinal)) return 2;
                previous = translated;
            }
            return UpdateVersion.Current(typeof(App).Assembly) > new Version(0, 0, 0) ? 0 : 3;
        }
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
