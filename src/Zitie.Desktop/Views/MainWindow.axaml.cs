using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Media;
using Avalonia.Interactivity;
using System.Diagnostics;
using System.Reflection;
using Zitie.Desktop.Controls;
using Zitie.Desktop.Services;
using CodeWF.Toolkit.Core.UpdateChecking;

namespace Zitie.Desktop.Views;

public partial class MainWindow : ZitieWindow
{
    private static readonly UpdateChecker UpdateChecker = new("dotnet9", "Zitie");
    private WindowNotificationManager? _notificationManager;
    private bool _checkingUpdate;

    private void Notify(string message)
    {
        // Avalonia 12 的通知管理器直接收 NotificationCard
        _notificationManager?.Show(new NotificationCard
        {
            Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, MaxWidth = 280 }
        });
    }

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        _notificationManager = new WindowNotificationManager(this) { Position = NotificationPosition.TopRight };
    }

    private async void OnCheckUpdate(object? sender, RoutedEventArgs e)
    {
        if (_notificationManager is null || _checkingUpdate)
        {
            return;
        }

        _checkingUpdate = true;
        Notify("正在检查更新…");
        try
        {
            Version? current = Assembly.GetEntryAssembly()?.GetName().Version;
            UpdateCheckResult result = await UpdateChecker.CheckAsync(current ?? new Version(0, 1, 0));
            if (!result.Succeeded)
            {
                Notify($"检查更新失败：{result.Error}");
                return;
            }

            if (result.Update is { } update)
            {
                // 仅提醒不自动下载：提示并打开发布页
                Notify($"发现新版本 {update.Tag}，已打开发布页");
                Process.Start(new ProcessStartInfo(update.PageUrl) { UseShellExecute = true });
                return;
            }

            Notify("当前已是最新版本");
        }
        catch (Exception ex)
        {
            Notify($"检查更新失败：{ex.Message}");
        }
        finally
        {
            _checkingUpdate = false;
        }
    }
}
