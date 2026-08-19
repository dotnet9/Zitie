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
            OpenWithSystem(path);
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
        }
        catch (Exception exception)
        {
            ZitieLogging.Error("导出 PNG 失败", exception);
        }
    }

    private void PrintButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { CanExport: true } viewModel) return;

        var path = Path.Combine(Path.GetTempPath(), $"zitie-print-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
        try
        {
            viewModel.ExportPdfTo(path);
            PrintPdf(path);
        }
        catch (Exception exception)
        {
            ZitieLogging.Error("打印失败", exception);
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

    private static void PrintPdf(string path)
    {
        // 先尝试系统“打印”动词，不支持打印关联的系统退回直接打开文件
        try
        {
            using var process = Process.Start(new ProcessStartInfo(path)
            {
                Verb = "print",
                UseShellExecute = true
            });
            if (process is not null) return;
        }
        catch (Exception exception)
        {
            ZitieLogging.Warn($"系统打印动词不可用，改为打开文件：{path}", exception);
        }

        OpenWithSystem(path);
    }

    private static void OpenWithSystem(string path)
    {
        try
        {
            using var _ = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            ZitieLogging.Error($"无法打开文件：{path}", exception);
        }
    }
}
