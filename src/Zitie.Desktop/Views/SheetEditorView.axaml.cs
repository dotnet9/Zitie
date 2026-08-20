using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Zitie.Avalonia.Export;
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

    private async void OpenDocumentButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } viewModel) return;
        var path = await PickOpenPathAsync("字帖文档", ["*.zitie.json", "*.json"]);
        if (path is null) return;

        try
        {
            viewModel.LoadDocumentFrom(path);
        }
        catch (Exception exception)
        {
            SetFailure(viewModel, "打开文档失败", exception);
        }
    }

    private async void SaveDocumentButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } viewModel) return;
        if (string.IsNullOrWhiteSpace(viewModel.DocumentPath))
        {
            await SaveAsDocumentAsync(viewModel);
            return;
        }

        try
        {
            viewModel.SaveDocumentTo(viewModel.DocumentPath);
        }
        catch (Exception exception)
        {
            SetFailure(viewModel, "保存文档失败", exception);
        }
    }

    private async void SaveAsDocumentButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is { } viewModel) await SaveAsDocumentAsync(viewModel);
    }

    private async Task SaveAsDocumentAsync(SheetEditorViewModel viewModel)
    {
        var path = await PickSavePathAsync("字帖文档", "zitie.json", "未命名字帖.zitie.json");
        if (path is null) return;

        try
        {
            viewModel.SaveDocumentTo(path);
        }
        catch (Exception exception)
        {
            SetFailure(viewModel, "保存文档失败", exception);
        }
    }

    private async void SaveTemplateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } viewModel) return;
        var path = await PickSavePathAsync("字帖模板", "json", "我的字帖模板.json");
        if (path is null) return;

        try
        {
            viewModel.SaveTemplateTo(path);
        }
        catch (Exception exception)
        {
            SetFailure(viewModel, "保存模板失败", exception);
        }
    }

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
            SetFailure(viewModel, "导出 PDF 失败", exception);
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
            SetFailure(viewModel, "导出 PNG 失败", exception);
        }
    }

    private async void ExportPngsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { CanExport: true } viewModel) return;
        var directory = await PickFolderPathAsync("选择 PNG 输出目录");
        if (directory is null) return;

        try
        {
            viewModel.ExportPngsTo(directory);
            OpenFolder(directory);
        }
        catch (Exception exception)
        {
            SetFailure(viewModel, "批量导出 PNG 失败", exception);
        }
    }

    private async void PrintButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { CanExport: true } viewModel) return;

        var path = Path.Combine(Path.GetTempPath(), $"zitie-print-{Guid.NewGuid():N}.pdf");
        try
        {
            viewModel.ExportPdfTo(path);
            SheetPrinter.PrintPdf(path);
            viewModel.SetStatus("已发送到系统打印队列");
        }
        catch (Exception exception)
        {
            SetFailure(viewModel, "打印失败", exception);
        }
    }

    private async Task<string?> PickSavePathAsync(string typeName, string extension, string suggestedName)
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

    private async Task<string?> PickOpenPathAsync(string typeName, IReadOnlyList<string> patterns)
    {
        var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storageProvider is null) return null;

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = "打开字帖文档",
            FileTypeFilter =
            [
                new FilePickerFileType(typeName)
                {
                    Patterns = patterns
                }
            ]
        });
        return files.FirstOrDefault()?.TryGetLocalPath();
    }

    private async Task<string?> PickFolderPathAsync(string title)
    {
        var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storageProvider is null) return null;

        var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            Title = title
        });
        return folders.FirstOrDefault()?.TryGetLocalPath();
    }

    private static void SetFailure(SheetEditorViewModel viewModel, string message, Exception exception)
    {
        viewModel.SetStatus($"{message}：{exception.Message}");
        ZitieLogging.Error(message, exception);
    }

    private static void OpenFolderAndSelectFile(string fileFullName)
    {
        if (!File.Exists(fileFullName)) return;
        var normalizedPath = NormalizePathSeparators(fileFullName);
        try
        {
            if (OperatingSystem.IsWindows())
                Process.Start(new ProcessStartInfo("Explorer.exe")
                {
                    Arguments = $"/e,/select,\"{normalizedPath}\""
                });
            else if (OperatingSystem.IsLinux())
                OpenFolder(Path.GetDirectoryName(normalizedPath) ?? ".");
            else if (OperatingSystem.IsMacOS())
                Process.Start(new ProcessStartInfo("open")
                {
                    Arguments = $"-R \"{normalizedPath}\"",
                    UseShellExecute = false
                });
        }
        catch (Exception exception)
        {
            ZitieLogging.Warn($"无法定位导出文件：{fileFullName}", exception);
        }
    }

    private static void OpenFolder(string folder)
    {
        try
        {
            if (OperatingSystem.IsWindows())
                Process.Start(new ProcessStartInfo("Explorer.exe") { Arguments = folder });
            else if (OperatingSystem.IsLinux())
                Process.Start(new ProcessStartInfo("xdg-open") { ArgumentList = { folder }, UseShellExecute = false });
            else if (OperatingSystem.IsMacOS())
                Process.Start(new ProcessStartInfo("open") { ArgumentList = { folder }, UseShellExecute = false });
        }
        catch (Exception exception)
        {
            ZitieLogging.Warn($"无法打开文件夹：{folder}", exception);
        }
    }

    private static string NormalizePathSeparators(string path)
    {
        var target = Path.DirectorySeparatorChar;
        var source = target == '\\' ? '/' : '\\';
        return path.Replace(source, target);
    }
}
