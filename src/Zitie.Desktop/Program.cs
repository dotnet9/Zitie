using System;
using Avalonia;

namespace Zitie.Desktop;

internal static class Program
{
    // Initialization code. Don't remove; also used by visual designer.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
