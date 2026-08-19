using Avalonia.Media;
using CodeWF.Log.Avalonia;
using CodeWF.Log.Core;
using Microsoft.Extensions.Logging;

namespace Zitie.Desktop.Services;

/// <summary>
///     日志门面：事件流供日志视图消费，默认隐藏，由主窗口菜单打开。
/// </summary>
internal static class ZitieLogging
{
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;

        Logger.TryInitialize(new LoggerOptions
        {
            MinimumLevel = LogLevel.Debug,
            EnableConsole = false,
            EnableEventFeed = true,
            RecentEventCapacity = 2_000,
            LineTemplate = "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message}{NewLine}"
        });
        _initialized = true;
        Info("Zitie 日志初始化完成");
    }

    public static LogView CreateView()
    {
        return new LogView
        {
            Source = Logger.Events,
            MinimumLevel = LogLevel.Debug,
            MaximumLevel = LogLevel.Critical,
            MaxDisplayCount = 1_000,
            RefreshInterval = TimeSpan.FromMilliseconds(80),
            TimestampFormat = "HH:mm:ss.fff",
            Background = Brushes.Transparent
        };
    }

    public static void Info(string message)
    {
        EnsureInitialized();
        Logger.Info(message);
    }

    public static void Warn(string message, Exception? exception = null)
    {
        EnsureInitialized();
        Logger.Warn(message, exception);
    }

    public static void Error(string message, Exception? exception = null)
    {
        EnsureInitialized();
        Logger.Error(message, exception);
    }

    private static void EnsureInitialized()
    {
        if (_initialized) Initialize();
    }
}
