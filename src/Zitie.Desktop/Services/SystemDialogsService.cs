using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace Zitie.Desktop.Services;

/// <summary>
///     系统对话框门面：文件/文件夹选择与资源管理器定位，供 ViewModel 调用，
///     使视图无需在代码后置中处理交互。
/// </summary>
public interface ISystemDialogs
{
    Task<string?> PickOpenFileAsync(string typeName, IReadOnlyList<string> patterns, string title = "打开文件");

    Task<string?> PickSaveFileAsync(string typeName, string extension, string suggestedName);

    Task<string?> PickFolderAsync(string title);

    /// <summary>在系统文件管理器中打开目录并选中文件（定位导出结果）。</summary>
    void RevealFile(string fileFullName);

    /// <summary>用系统文件管理器打开目录。</summary>
    void OpenFolder(string folderFullName);
}

public sealed class SystemDialogsService : ISystemDialogs
{
    public async Task<string?> PickOpenFileAsync(string typeName, IReadOnlyList<string> patterns, string title = "打开文件")
    {
        var storageProvider = ResolveStorageProvider();
        if (storageProvider is null) return null;

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = title,
            FileTypeFilter =
            [
                new FilePickerFileType(typeName) { Patterns = patterns }
            ]
        });
        return files.FirstOrDefault()?.TryGetLocalPath();
    }

    public async Task<string?> PickSaveFileAsync(string typeName, string extension, string suggestedName)
    {
        var storageProvider = ResolveStorageProvider();
        if (storageProvider is null) return null;

        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = suggestedName,
            DefaultExtension = extension,
            FileTypeChoices =
            [
                new FilePickerFileType(typeName) { Patterns = [$"*.{extension}"] }
            ]
        });
        return file?.TryGetLocalPath();
    }

    public async Task<string?> PickFolderAsync(string title)
    {
        var storageProvider = ResolveStorageProvider();
        if (storageProvider is null) return null;

        var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            Title = title
        });
        return folders.FirstOrDefault()?.TryGetLocalPath();
    }

    public void RevealFile(string fileFullName)
    {
        if (!File.Exists(fileFullName)) return;
        var normalizedPath = NormalizePathSeparators(fileFullName);
        try
        {
            if (OperatingSystem.IsWindows())
                Process.Start(new ProcessStartInfo("Explorer.exe") { Arguments = $"/e,/select,\"{normalizedPath}\"" });
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

    public void OpenFolder(string folderFullName)
    {
        try
        {
            if (OperatingSystem.IsWindows())
                Process.Start(new ProcessStartInfo("Explorer.exe") { Arguments = folderFullName });
            else if (OperatingSystem.IsLinux())
                Process.Start(new ProcessStartInfo("xdg-open") { ArgumentList = { folderFullName }, UseShellExecute = false });
            else if (OperatingSystem.IsMacOS())
                Process.Start(new ProcessStartInfo("open") { ArgumentList = { folderFullName }, UseShellExecute = false });
        }
        catch (Exception exception)
        {
            ZitieLogging.Warn($"无法打开文件夹：{folderFullName}", exception);
        }
    }

    private static IStorageProvider? ResolveStorageProvider()
    {
        return (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
            ?.MainWindow?.StorageProvider;
    }

    private static string NormalizePathSeparators(string path)
    {
        var target = Path.DirectorySeparatorChar;
        var source = target == '\\' ? '/' : '\\';
        return path.Replace(source, target);
    }
}
