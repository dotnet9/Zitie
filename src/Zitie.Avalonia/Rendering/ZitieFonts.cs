using SkiaSharp;

namespace Zitie.Avalonia.Rendering;

/// <summary>
///     内嵌字体访问。霞鹜文楷以 SIL OFL 1.1 协议分发，许可证见包内 licenses 目录。
/// </summary>
public static class ZitieFonts
{
    public const string WenKaiFamilyName = "LXGW WenKai";

    private static MemoryStream? _wenKaiData;
    private static SKTypeface? _wenKai;

    /// <summary>霞鹜文楷常规体。</summary>
    public static SKTypeface WenKai
    {
        get
        {
            if (_wenKai is not null) return _wenKai;

            var assembly = typeof(ZitieFonts).Assembly;
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.Contains("LXGWWenKai", StringComparison.OrdinalIgnoreCase));
            if (resourceName is null)
                throw new InvalidOperationException("未找到内嵌的霞鹜文楷字体资源。");

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
                throw new InvalidOperationException($"无法读取内嵌资源 {resourceName}。");

            _wenKaiData = new MemoryStream();
            stream.CopyTo(_wenKaiData);
            _wenKaiData.Position = 0;
            _wenKai = SKTypeface.FromStream(_wenKaiData);
            return _wenKai;
        }
    }
}
