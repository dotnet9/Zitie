using System.Diagnostics;

namespace Zitie.Avalonia.Export;

/// <summary>调用操作系统默认 PDF 打印程序。</summary>
public static class SheetPrinter
{
    public static void PrintPdf(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path)) throw new FileNotFoundException("待打印的 PDF 不存在。", path);

        ProcessStartInfo startInfo;
        if (OperatingSystem.IsWindows())
        {
            startInfo = new ProcessStartInfo(path)
            {
                UseShellExecute = true,
                Verb = "print"
            };
        }
        else
        {
            var command = OperatingSystem.IsMacOS() ? "lp" : "lp";
            startInfo = new ProcessStartInfo(command)
            {
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add(path);
        }

        using var process = Process.Start(startInfo);
        if (process is null) throw new InvalidOperationException("无法启动系统打印程序。");
    }
}
