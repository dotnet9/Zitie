using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Zitie.Desktop.Services;
using Zitie.Desktop.ViewModels;

namespace Zitie.Desktop.Views;

public partial class SheetEditorView : UserControl
{
    public SheetEditorView()
    {
        InitializeComponent();
    }

    private SheetEditorViewModel? ViewModel => DataContext as SheetEditorViewModel;

    private async void ExportPdfButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { CanExport: true } viewModel) return;

        var path = await PickSavePathAsync("PDF 文档", "pdf", $"zitie-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
        if (path is null) return;

        try
        {
            viewModel.ExportPdfTo(path);
            OpenFolderAndSelectFile(path);
        }
        catch (Exception exception)
        {
            ZitieLogging.Error("导出 PDF 失败", exception);
        }
    }

    private async void ExportPngButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { CanExport: true } viewModel) return;

        var path = await PickSavePathAsync("PNG 图片", "png", $"zitie-{DateTime.Now:yyyyMMdd-HHmmss}.png");
        if (path is null) return;

        try
        {
            viewModel.ExportPngTo(path);
            OpenFolderAndSelectFile(path);
        }
        catch (Exception exception)
        {
            ZitieLogging.Error("导出 PNG 失败", exception);
        }
    }

    private async System.Threading.Tasks.Task<string?> PickSavePathAsync(
        string typeName,
        string extension,
        string suggestedName)
    {
        var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storageProvider is null) return null;

        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = suggestedName,
            DefaultExtension = extension,
            FileTypeChoices =
            [
                new FilePickerFileType(typeName)
                {
                    Patterns = [$"*.{extension}"]
                }
            ]
        });

        return file?.TryGetLocalPath();
    }

    /// <summary>
    ///     打开资源管理器并选中导出文件（参考 CodeWF.Tools 的 FileHelper）。
    ///     Windows 用 Explorer /select；Linux/macOS 分别用对应文件管理器。
    /// </summary>
    private static void OpenFolderAndSelectFile(string fileFullName)
    {
        if (!File.Exists(fileFullName))
        {
            ZitieLogging.Warn($"导出文件不存在，无法定位：{fileFullName}");
            return;
        }

        var normalizedPath = NormalizePathSeparators(fileFullName);
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo("Explorer.exe")
                {
                    Arguments = $"/e,/select,\"{normalizedPath}\""
                });
            }
            else if (OperatingSystem.IsLinux())
            {
                // 常见文件管理器优先，未知桌面环境退回打开所在目录
                var desktop = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP")?.ToLower() ?? "";
                var (manager, args) = desktop switch
                {
                    var d when d.Contains("gnome") || d.Contains("unity") => ("nautilus", $"--select \"{normalizedPath}\""),
                    var d when d.Contains("kde") => ("dolphin", $"--select \"{normalizedPath}\""),
                    var d when d.Contains("xfce") => ("thunar", $"--select \"{normalizedPath}\""),
                    var d when d.Contains("mate") => ("caja", $"--select \"{normalizedPath}\""),
                    var d when d.Contains("lxqt") => ("pcmanfm-qt", $"--select \"{normalizedPath}\""),
                    _ => ("xdg-open", $"\"{Path.GetDirectoryName(normalizedPath) ?? "."}\"")
                };
                Process.Start(new ProcessStartInfo(manager)
                {
                    Arguments = args,
                    UseShellExecute = false
                });
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start(new ProcessStartInfo("open")
                {
                    Arguments = $"-R \"{normalizedPath}\"",
                    UseShellExecute = false
                });
            }
            else
            {
                var folder = Path.GetDirectoryName(Path.GetFullPath(normalizedPath));
                if (folder is not null) OpenFolder(folder);
            }
        }
        catch (Exception exception)
        {
            ZitieLogging.Error($"无法定位导出文件：{fileFullName}", exception);
        }
    }

    /// <summary>仅打开目录（未知系统兜底）。</summary>
    private static void OpenFolder(string folder)
    {
        try
        {
            if (OperatingSystem.IsWindows())
                Process.Start(new ProcessStartInfo("Explorer.exe") { Arguments = folder });
            else if (OperatingSystem.IsLinux())
                Process.Start(new ProcessStartInfo("xdg-open") { Arguments = $"\"{folder}\"", UseShellExecute = false });
            else if (OperatingSystem.IsMacOS())
                Process.Start(new ProcessStartInfo("open") { Arguments = $"\"{folder}\"", UseShellExecute = false });
        }
        catch (Exception exception)
        {
            ZitieLogging.Error($"无法打开文件夹：{folder}", exception);
        }
    }

    /// <summary>统一路径分隔符为系统标准。</summary>
    private static string NormalizePathSeparators(string path)
    {
        var target = Path.DirectorySeparatorChar;
        var source = target == '\\' ? '/' : '\\';
        return path.Replace(source, target);
    }
}
