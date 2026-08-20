using SkiaSharp;
using Zitie.Avalonia.Rendering;
using Zitie.Avalonia.Themes;
using Zitie.Core.Layout;
using Zitie.Core.Models;

namespace Zitie.Avalonia.Export;

/// <summary>
///     字帖导出：PDF（矢量、字体子集内嵌）与 PNG（默认 300dpi）。
/// </summary>
public static class SheetExporter
{
    public static void ExportPdf(
        string path,
        CharacterSheetSpec spec,
        IReadOnlyList<SheetPage> pages,
        SheetRenderTheme? theme = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(pages);
        theme ??= SheetRenderThemes.Print;

        using var stream = File.Create(path);
        using var document = SKDocument.CreatePdf(stream);
        var widthPt = (float)(spec.Page.WidthMm * LayoutEngine.MmToPt);
        var heightPt = (float)(spec.Page.HeightMm * LayoutEngine.MmToPt);

        foreach (var page in pages)
        {
            var canvas = document.BeginPage(widthPt, heightPt);
            SheetRenderer.RenderPage(canvas, spec, page, pages.Count, theme);
            document.EndPage();
        }

        document.Close();
    }

    public static void ExportPng(
        string path,
        CharacterSheetSpec spec,
        SheetPage page,
        int totalPages,
        double dpi = 300,
        SheetRenderTheme? theme = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(page);
        if (double.IsNaN(dpi) || double.IsInfinity(dpi) || dpi <= 0)
            throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "DPI 必须是正数。");

        theme ??= SheetRenderThemes.Print;

        var pxPerMm = dpi / 25.4;
        var width = Math.Max(1, (int)Math.Ceiling(spec.Page.WidthMm * pxPerMm));
        var height = Math.Max(1, (int)Math.Ceiling(spec.Page.HeightMm * pxPerMm));

        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque);
        using var surface = SKSurface.Create(info);
        if (surface is null)
            throw new InvalidOperationException($"无法创建 {width}x{height} 的渲染表面。");

        surface.Canvas.Scale((float)(pxPerMm / LayoutEngine.MmToPt));
        SheetRenderer.RenderPage(surface.Canvas, spec, page, totalPages, theme);
        surface.Flush();

        using var snapshot = surface.Snapshot();
        using var encoded = snapshot.Encode(SKEncodedImageFormat.Png, 100);
        using var fileStream = File.Create(path);
        encoded.SaveTo(fileStream);
    }
}
